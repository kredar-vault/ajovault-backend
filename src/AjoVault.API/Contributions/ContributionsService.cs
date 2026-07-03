using AjoVault.API.Contributions.Dto;
using AjoVault.API.Groups;

namespace AjoVault.API.Contributions;

public class ContributionsService(ContributionRepository contributionRepo, GroupRepository groupRepo)
{
    public async Task<ContributionResponse> RecordAsync(Guid userId, Guid groupId, RecordContributionRequest request)
    {
        var group = await groupRepo.FindByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Savings group not found.");

        if (group.Status != GroupStatus.Active)
            throw new InvalidOperationException("This savings group is not active yet.");

        var member = await groupRepo.FindMemberAsync(groupId, userId)
            ?? throw new UnauthorizedAccessException("You are not a member of this group.");

        var existing = await contributionRepo.FindAsync(groupId, userId, request.CycleNumber);
        if (existing != null)
            throw new InvalidOperationException("You have already contributed for this cycle.");

        var contribution = new Contribution
        {
            GroupId = groupId,
            UserId = userId,
            CycleNumber = request.CycleNumber,
            Amount = group.ContributionAmount
        };

        await contributionRepo.AddAsync(contribution);
        return MapToResponse(contribution);
    }

    public async Task<List<ContributionResponse>> GetByGroupAsync(Guid groupId) =>
        (await contributionRepo.GetByGroupAsync(groupId)).Select(MapToResponse).ToList();

    public async Task<List<ContributionResponse>> GetMineAsync(Guid groupId, Guid userId) =>
        (await contributionRepo.GetByUserAsync(groupId, userId)).Select(MapToResponse).ToList();

    private static ContributionResponse MapToResponse(Contribution c) => new()
    {
        Id = c.Id,
        GroupId = c.GroupId,
        UserId = c.UserId,
        CycleNumber = c.CycleNumber,
        Amount = c.Amount,
        PaidAt = c.PaidAt
    };
}
