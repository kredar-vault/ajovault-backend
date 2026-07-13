using AjoVault.API.Auth;
using AjoVault.API.Contributions;
using AjoVault.API.Groups;
using AjoVault.API.Payouts.Dto;

namespace AjoVault.API.Payouts;

public class PayoutsService(PayoutRepository payoutRepo, GroupRepository groupRepo, UserRepository userRepo, ContributionRepository contributionRepo)
{
    public async Task<List<RotationMemberResponse>> GetByGroupAsync(Guid groupId)
    {
        var payouts = await payoutRepo.GetByGroupAsync(groupId);
        var userIds = payouts.Select(p => p.RecipientUserId).Distinct();
        var users = await userRepo.FindByIdsAsync(userIds);
        var userLookup = users.ToDictionary(u => u.Id, u => u.FullName);

        var ordered = payouts.OrderBy(p => p.CycleNumber).ToList();
        var firstScheduledIndex = ordered.FindIndex(p => p.Status == PayoutStatus.Scheduled);

        return ordered.Select((p, idx) =>
        {
            var name = userLookup.GetValueOrDefault(p.RecipientUserId, "Unknown");
            var status = p.Status == PayoutStatus.Disbursed ? "PAID"
                : idx == firstScheduledIndex ? "CURRENT"
                : "WAITING";

            string? dateInfo = status switch
            {
                "CURRENT" => p.ScheduledDate > DateTime.UtcNow
                    ? $"Receiving in {Math.Max(0, (p.ScheduledDate - DateTime.UtcNow).Days)} days"
                    : "Due now",
                "WAITING" => p.ScheduledDate.ToString("MMMM yyyy"),
                "PAID" => p.DisbursedAt?.ToString("MMM d, yyyy"),
                _ => null
            };

            return new RotationMemberResponse
            {
                Id = p.Id.ToString(),
                Name = name,
                Initials = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => w[0])).ToUpper(),
                Position = p.CycleNumber,
                Amount = p.Amount,
                Status = status,
                DateInfo = dateInfo
            };
        }).ToList();
    }

    public async Task<List<PayoutResponse>> GetUpcomingByUserAsync(Guid userId)
    {
        var payouts = await payoutRepo.GetUpcomingByUserAsync(userId);
        var groupIds = payouts.Select(p => p.GroupId).Distinct();
        var groups = await groupRepo.GetByMemberAsync(userId);
        var groupLookup = groups.ToDictionary(g => g.Id, g => g.Name);
        var user = await userRepo.FindByIdAsync(userId);
        var userName = user?.FullName ?? "Unknown";

        return payouts.Select(p => MapToResponse(p, groupLookup.GetValueOrDefault(p.GroupId, ""), userName)).ToList();
    }

    public async Task<PayoutResponse> DisburseAsync(Guid userId, Guid groupId, Guid payoutId)
    {
        var group = await groupRepo.FindByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Savings group not found.");

        if (group.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Only the group creator can disburse payouts.");

        var payout = await payoutRepo.FindByIdAsync(groupId, payoutId)
            ?? throw new KeyNotFoundException("Payout not found.");

        if (payout.Status == PayoutStatus.Disbursed)
            throw new InvalidOperationException("This payout has already been disbursed.");

        // All members must have contributed for this cycle before disbursing
        var members = await groupRepo.GetMembersAsync(groupId);
        var cycleContributions = await contributionRepo.GetByCycleAsync(groupId, payout.CycleNumber);
        var paidMemberCount = cycleContributions.Count(c => c.Status == ContributionStatus.Received);

        if (paidMemberCount < members.Count)
            throw new InvalidOperationException(
                $"Cannot disburse: only {paidMemberCount} of {members.Count} members have contributed for cycle {payout.CycleNumber}. " +
                $"All members must contribute before the payout is released.");

        // Circle balance must cover this payout
        var allContributions = await contributionRepo.GetByGroupAsync(groupId);
        var allPayouts = await payoutRepo.GetByGroupAsync(groupId);
        var totalContributed = allContributions.Where(c => c.Status == ContributionStatus.Received).Sum(c => c.Amount);
        var totalDisbursed = allPayouts.Where(p => p.Status == PayoutStatus.Disbursed).Sum(p => p.Amount);
        var circleBalance = totalContributed - totalDisbursed;

        if (circleBalance < payout.Amount)
            throw new InvalidOperationException(
                $"Insufficient circle balance. Pool has ₦{circleBalance:N2} but payout is ₦{payout.Amount:N2}.");

        payout.Status = PayoutStatus.Disbursed;
        payout.DisbursedAt = DateTime.UtcNow;
        await payoutRepo.UpdateAsync(payout);

        var recipient = await userRepo.FindByIdAsync(payout.RecipientUserId);
        return MapToResponse(payout, group.Name, recipient?.FullName ?? "Unknown");
    }

    public async Task<PayoutSummaryResponse> GetCurrentPayoutAsync(Guid groupId)
    {
        var payouts = await payoutRepo.GetByGroupAsync(groupId);
        var members = await groupRepo.GetMembersAsync(groupId);
        var ordered = payouts.OrderBy(p => p.CycleNumber).ToList();

        var scheduled = ordered.Where(p => p.Status == PayoutStatus.Scheduled).ToList();
        var current = scheduled.FirstOrDefault();
        var next = scheduled.Skip(1).FirstOrDefault();

        var payoutsDone = ordered.Count(p => p.Status == PayoutStatus.Disbursed);

        User? currentRecipient = current != null ? await userRepo.FindByIdAsync(current.RecipientUserId) : null;
        User? nextRecipient = next != null ? await userRepo.FindByIdAsync(next.RecipientUserId) : null;

        return new PayoutSummaryResponse
        {
            CurrentCycle = current?.CycleNumber.ToString() ?? "N/A",
            CurrentRecipientName = currentRecipient?.FullName ?? "None",
            NextRecipientName = nextRecipient?.FullName ?? "None",
            TotalMembers = members.Count,
            PayoutsDone = payoutsDone
        };
    }

    public async Task<List<PayoutResponse>> GetUpcomingByGroupAsync(Guid groupId)
    {
        var payouts = await payoutRepo.GetByGroupAsync(groupId);
        var group = await groupRepo.FindByIdAsync(groupId);
        var userIds = payouts.Select(p => p.RecipientUserId).Distinct();
        var users = await userRepo.FindByIdsAsync(userIds);
        var userLookup = users.ToDictionary(u => u.Id, u => u.FullName);

        return payouts
            .Where(p => p.Status == PayoutStatus.Scheduled)
            .Select(p => MapToResponse(p, group?.Name ?? "", userLookup.GetValueOrDefault(p.RecipientUserId, "Unknown")))
            .ToList();
    }

    private static PayoutResponse MapToResponse(Payout p, string groupName, string recipientName) => new()
    {
        Id = p.Id,
        GroupId = p.GroupId,
        GroupName = groupName,
        CycleNumber = p.CycleNumber,
        RecipientUserId = p.RecipientUserId,
        RecipientName = recipientName,
        Amount = p.Amount,
        ScheduledDate = p.ScheduledDate,
        Status = p.Status.ToString(),
        DisbursedAt = p.DisbursedAt,
        DaysUntil = p.Status == PayoutStatus.Scheduled
            ? Math.Max(0, (p.ScheduledDate - DateTime.UtcNow).Days)
            : 0
    };
}
