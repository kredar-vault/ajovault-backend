using AjoVault.API.Auth;
using AjoVault.API.Groups;
using AjoVault.API.Payouts.Dto;

namespace AjoVault.API.Payouts;

public class PayoutsService(PayoutRepository payoutRepo, GroupRepository groupRepo, UserRepository userRepo)
{
    public async Task<List<PayoutResponse>> GetByGroupAsync(Guid groupId)
    {
        var payouts = await payoutRepo.GetByGroupAsync(groupId);
        var group = await groupRepo.FindByIdAsync(groupId);
        var userIds = payouts.Select(p => p.RecipientUserId).Distinct();
        var users = await userRepo.FindByIdsAsync(userIds);
        var userLookup = users.ToDictionary(u => u.Id, u => u.FullName);

        return payouts.Select(p => MapToResponse(p, group?.Name ?? "", userLookup.GetValueOrDefault(p.RecipientUserId, "Unknown"))).ToList();
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

        payout.Status = PayoutStatus.Disbursed;
        payout.DisbursedAt = DateTime.UtcNow;
        await payoutRepo.UpdateAsync(payout);

        var recipient = await userRepo.FindByIdAsync(payout.RecipientUserId);
        return MapToResponse(payout, group.Name, recipient?.FullName ?? "Unknown");
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
