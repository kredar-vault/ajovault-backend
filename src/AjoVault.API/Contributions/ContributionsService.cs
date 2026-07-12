using AjoVault.API.Auth;
using AjoVault.API.Contributions.Dto;
using AjoVault.API.Groups;
using AjoVault.API.Payouts;

namespace AjoVault.API.Contributions;

public class ContributionsService(
    ContributionRepository contributionRepo,
    GroupRepository groupRepo,
    UserRepository userRepo,
    PayoutRepository payoutRepo)
{
    public async Task<ContributionResponse> RecordAsync(Guid userId, Guid groupId, RecordContributionRequest request)
    {
        var group = await groupRepo.FindByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Savings group not found.");

        if (group.Status != GroupStatus.Active)
            throw new InvalidOperationException("This savings group is not active yet.");

        var membership = await groupRepo.FindMemberAsync(groupId, userId)
            ?? throw new UnauthorizedAccessException("You are not a member of this group.");
        _ = membership;

        int cycle;
        if (request.CycleNumber is > 0)
        {
            cycle = request.CycleNumber.Value;
        }
        else
        {
            var payouts = await payoutRepo.GetByGroupAsync(groupId);
            var current = payouts.FirstOrDefault(p => p.Status == PayoutStatus.Scheduled);
            if (current == null)
                throw new InvalidOperationException("No active cycle found for this group.");
            cycle = current.CycleNumber;
        }

        var existing = await contributionRepo.FindAsync(groupId, userId, cycle);
        if (existing != null)
            throw new InvalidOperationException("You have already contributed for this cycle.");

        var user = await userRepo.FindByIdAsync(userId);
        var reference = GenerateReference();

        var contribution = new Contribution
        {
            GroupId = groupId,
            UserId = userId,
            CycleNumber = cycle,
            Amount = group.ContributionAmount,
            Status = ContributionStatus.Received,
            Reference = reference
        };

        await contributionRepo.AddAsync(contribution);
        return MapToResponse(contribution, group.Name, user?.FullName ?? "Unknown");
    }

    public async Task<List<ContributionResponse>> GetByGroupAsync(Guid groupId)
    {
        var contributions = await contributionRepo.GetByGroupAsync(groupId);
        var group = await groupRepo.FindByIdAsync(groupId);
        var userIds = contributions.Select(c => c.UserId).Distinct();
        var users = await userRepo.FindByIdsAsync(userIds);
        var userLookup = users.ToDictionary(u => u.Id, u => u.FullName);

        return contributions
            .Select(c => MapToResponse(c, group?.Name ?? "", userLookup.GetValueOrDefault(c.UserId, "Unknown")))
            .ToList();
    }

    public async Task<List<ContributionResponse>> GetMineAsync(Guid groupId, Guid userId)
    {
        var contributions = await contributionRepo.GetByUserAsync(groupId, userId);
        var group = await groupRepo.FindByIdAsync(groupId);
        var user = await userRepo.FindByIdAsync(userId);

        return contributions
            .Select(c => MapToResponse(c, group?.Name ?? "", user?.FullName ?? "Unknown"))
            .ToList();
    }

    public async Task<List<ContributionResponse>> GetAllByUserAsync(Guid userId)
    {
        var groups = await groupRepo.GetByMemberAsync(userId);
        var groupIds = groups.Select(g => g.Id).ToList();
        var groupLookup = groups.ToDictionary(g => g.Id, g => g.Name);

        var contributions = await contributionRepo.GetAllByUserGroupsAsync(groupIds);
        var userIds = contributions.Select(c => c.UserId).Distinct();
        var users = await userRepo.FindByIdsAsync(userIds);
        var userLookup = users.ToDictionary(u => u.Id, u => u.FullName);

        return contributions
            .Select(c => MapToResponse(
                c,
                groupLookup.GetValueOrDefault(c.GroupId, ""),
                userLookup.GetValueOrDefault(c.UserId, "Unknown")))
            .ToList();
    }

    private static ContributionResponse MapToResponse(Contribution c, string groupName, string memberName) => new()
    {
        Id = c.Id,
        GroupId = c.GroupId,
        GroupName = groupName,
        UserId = c.UserId,
        MemberName = memberName,
        CycleNumber = c.CycleNumber,
        Amount = c.Amount,
        Status = c.Status.ToString(),
        Reference = c.Reference,
        PaidAt = c.PaidAt
    };

    private static string GenerateReference()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";
        var random = new Random();
        var suffix = new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        return $"REF-{suffix}";
    }
}
