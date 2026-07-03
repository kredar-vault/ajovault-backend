using AjoVault.API.Auth;
using AjoVault.API.Groups.Dto;
using AjoVault.API.Payouts;

namespace AjoVault.API.Groups;

public class GroupsService(GroupRepository groupRepo, UserRepository userRepo, PayoutRepository payoutRepo)
{
    public async Task<GroupResponse> CreateAsync(Guid userId, CreateGroupRequest request)
    {
        if (!Enum.TryParse<ContributionFrequency>(request.Frequency, true, out var frequency))
            throw new InvalidOperationException("Frequency must be 'Weekly' or 'Monthly'.");

        var group = new SavingsGroup
        {
            Name = request.Name,
            Description = request.Description,
            ContributionAmount = request.ContributionAmount,
            Frequency = frequency,
            MaxMembers = request.MaxMembers,
            CreatedByUserId = userId
        };

        await groupRepo.AddAsync(group);

        await groupRepo.AddMemberAsync(new GroupMember
        {
            GroupId = group.Id,
            UserId = userId,
            PayoutPosition = 1
        });

        return await MapToResponseAsync(group);
    }

    public async Task<List<GroupResponse>> GetAllAsync()
    {
        var groups = await groupRepo.GetAllAsync();
        return (await Task.WhenAll(groups.Select(MapToResponseAsync))).ToList();
    }

    public async Task<List<GroupResponse>> GetMyGroupsAsync(Guid userId)
    {
        var groups = await groupRepo.GetByMemberAsync(userId);
        return (await Task.WhenAll(groups.Select(MapToResponseAsync))).ToList();
    }

    public async Task<GroupDetailResponse> GetByIdAsync(Guid groupId)
    {
        var group = await groupRepo.FindByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Savings group not found.");

        var members = await groupRepo.GetMembersAsync(groupId);
        var users = await userRepo.FindByIdsAsync(members.Select(m => m.UserId));
        var userLookup = users.ToDictionary(u => u.Id, u => u.FullName);

        var response = new GroupDetailResponse
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            ContributionAmount = group.ContributionAmount,
            Frequency = group.Frequency.ToString(),
            MaxMembers = group.MaxMembers,
            CurrentMembers = members.Count,
            CreatedByUserId = group.CreatedByUserId,
            Status = group.Status.ToString(),
            StartDate = group.StartDate,
            CreatedAt = group.CreatedAt,
            Members = members.Select(m => new GroupMemberResponse
            {
                UserId = m.UserId,
                FullName = userLookup.GetValueOrDefault(m.UserId, "Unknown"),
                PayoutPosition = m.PayoutPosition,
                JoinedAt = m.JoinedAt
            }).ToList()
        };

        return response;
    }

    public async Task<GroupResponse> JoinAsync(Guid userId, Guid groupId)
    {
        var group = await groupRepo.FindByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Savings group not found.");

        if (group.Status != GroupStatus.Open)
            throw new InvalidOperationException("This savings group is no longer accepting new members.");

        var existingMember = await groupRepo.FindMemberAsync(groupId, userId);
        if (existingMember != null)
            throw new InvalidOperationException("You are already a member of this group.");

        var members = await groupRepo.GetMembersAsync(groupId);
        if (members.Count >= group.MaxMembers)
            throw new InvalidOperationException("This savings group is full.");

        var nextPosition = members.Count + 1;
        await groupRepo.AddMemberAsync(new GroupMember
        {
            GroupId = group.Id,
            UserId = userId,
            PayoutPosition = nextPosition
        });

        if (nextPosition == group.MaxMembers)
        {
            group.Status = GroupStatus.Active;
            group.StartDate = DateTime.UtcNow;
            await groupRepo.UpdateAsync(group);
            await GeneratePayoutScheduleAsync(group);
        }

        return await MapToResponseAsync(group);
    }

    private async Task GeneratePayoutScheduleAsync(SavingsGroup group)
    {
        var members = await groupRepo.GetMembersAsync(group.Id);
        var potAmount = group.ContributionAmount * group.MaxMembers;
        var scheduledDate = group.StartDate!.Value;

        var payouts = new List<Payout>();
        foreach (var member in members.OrderBy(m => m.PayoutPosition))
        {
            payouts.Add(new Payout
            {
                GroupId = group.Id,
                CycleNumber = member.PayoutPosition,
                RecipientUserId = member.UserId,
                Amount = potAmount,
                ScheduledDate = scheduledDate
            });

            scheduledDate = group.Frequency == ContributionFrequency.Weekly
                ? scheduledDate.AddDays(7)
                : scheduledDate.AddMonths(1);
        }

        await payoutRepo.AddRangeAsync(payouts);
    }

    private async Task<GroupResponse> MapToResponseAsync(SavingsGroup group)
    {
        var memberCount = (await groupRepo.GetMembersAsync(group.Id)).Count;
        return new GroupResponse
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            ContributionAmount = group.ContributionAmount,
            Frequency = group.Frequency.ToString(),
            MaxMembers = group.MaxMembers,
            CurrentMembers = memberCount,
            CreatedByUserId = group.CreatedByUserId,
            Status = group.Status.ToString(),
            StartDate = group.StartDate,
            CreatedAt = group.CreatedAt
        };
    }
}
