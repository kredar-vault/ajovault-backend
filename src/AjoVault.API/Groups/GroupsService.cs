using AjoVault.API.Auth;
using AjoVault.API.Config;
using AjoVault.API.Contributions;
using AjoVault.API.Data;
using AjoVault.API.Groups.Dto;
using AjoVault.API.Kredar;
using AjoVault.API.Payouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AjoVault.API.Groups;

public class GroupsService(
    GroupRepository groupRepo,
    UserRepository userRepo,
    PayoutRepository payoutRepo,
    ContributionRepository contributionRepo,
    KredarClient kredarClient,
    AppDbContext db,
    IOptions<AppSettings> appSettings,
    ILogger<GroupsService> logger)
{
    private readonly string _baseUrl = appSettings.Value.BaseUrl;

    public async Task<GroupResponse> CreateAsync(Guid userId, CreateGroupRequest request)
    {
        if (!Enum.TryParse<ContributionFrequency>(request.Frequency, true, out var frequency))
            throw new InvalidOperationException("Frequency must be 'Weekly', 'BiWeekly', or 'Monthly'.");

        var group = new SavingsGroup
        {
            Name = request.Name,
            Description = request.Description,
            PrimaryPurpose = request.PrimaryPurpose,
            ContributionAmount = request.ContributionAmount,
            Frequency = frequency,
            MaxMembers = request.MaxMembers,
            CreatedByUserId = userId,
            InviteCode = GenerateInviteCode(request.Name),
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
        };

        await groupRepo.AddAsync(group);

        await groupRepo.AddMemberAsync(new GroupMember
        {
            GroupId = group.Id,
            UserId = userId,
            PayoutPosition = 1,
            Role = GroupMemberRole.Admin
        });

        return await MapToResponseAsync(group);
    }

    public async Task<List<GroupResponse>> GetAllAsync()
    {
        var groups = await groupRepo.GetAllAsync();
        var result = new List<GroupResponse>();
        foreach (var g in groups) result.Add(await MapToResponseAsync(g));
        return result;
    }

    public async Task<List<GroupResponse>> GetMyGroupsAsync(Guid userId)
    {
        var groups = await groupRepo.GetByMemberAsync(userId);
        var result = new List<GroupResponse>();
        foreach (var g in groups) result.Add(await MapToResponseAsync(g));
        return result;
    }

    public async Task<GroupDetailResponse> GetByIdAsync(Guid groupId)
    {
        var group = await groupRepo.FindByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Savings group not found.");

        var members = await groupRepo.GetMembersAsync(groupId);
        var users = await userRepo.FindByIdsAsync(members.Select(m => m.UserId));
        var userLookup = users.ToDictionary(u => u.Id, u => u.FullName);

        var currentCycle = GetCurrentCycleNumber(group);
        var contributions = currentCycle > 0
            ? await contributionRepo.GetByGroupAsync(groupId)
            : [];

        var paidUserIds = contributions
            .Where(c => c.CycleNumber == currentCycle)
            .Select(c => c.UserId)
            .ToHashSet();

        var nextPayoutPosition = await GetNextPayoutPositionAsync(groupId);

        var memberCount = members.Count;
        var base_ = await MapToResponseBaseAsync(group, memberCount);
        var response = new GroupDetailResponse
        {
            Id = base_.Id,
            Name = base_.Name,
            Description = base_.Description,
            PrimaryPurpose = base_.PrimaryPurpose,
            ContributionAmount = base_.ContributionAmount,
            TotalPool = base_.TotalPool,
            Frequency = base_.Frequency,
            MaxMembers = base_.MaxMembers,
            CurrentMembers = base_.CurrentMembers,
            CreatedByUserId = base_.CreatedByUserId,
            Status = base_.Status,
            StartDate = base_.StartDate,
            CreatedAt = base_.CreatedAt,
            InviteCode = base_.InviteCode,
            InviteLink = base_.InviteLink,
            Members = members.Select(m => new GroupMemberResponse
            {
                UserId = m.UserId,
                FullName = userLookup.GetValueOrDefault(m.UserId, "Unknown"),
                PayoutPosition = m.PayoutPosition,
                JoinedAt = m.JoinedAt,
                ContributionStatus = currentCycle > 0
                    ? (paidUserIds.Contains(m.UserId) ? "Paid" : IsOverdue(group, currentCycle) ? "Missed" : "Pending")
                    : "Pending",
                IsNextPayout = m.PayoutPosition == nextPayoutPosition
            }).ToList()
        };

        return response;
    }

    public async Task<GroupResponse> JoinAsync(Guid userId, Guid groupId)
    {
        var group = await groupRepo.FindByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Savings group not found.");

        return await JoinGroupAsync(userId, group);
    }

    public async Task<GroupResponse> JoinByInviteCodeAsync(Guid userId, string inviteCode)
    {
        var group = await groupRepo.FindByInviteCodeAsync(inviteCode)
            ?? throw new KeyNotFoundException("Invalid invite code.");

        return await JoinGroupAsync(userId, group);
    }

    public async Task<string> GetInviteLinkAsync(Guid groupId)
    {
        var group = await groupRepo.FindByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Savings group not found.");

        return BuildInviteLink(group.InviteCode);
    }

    private async Task<GroupResponse> JoinGroupAsync(Guid userId, SavingsGroup group)
    {
        if (group.Status != GroupStatus.Open)
            throw new InvalidOperationException("This savings group is no longer accepting new members.");

        var existingMember = await groupRepo.FindMemberAsync(group.Id, userId);
        if (existingMember != null)
            throw new InvalidOperationException("You are already a member of this group.");

        var members = await groupRepo.GetMembersAsync(group.Id);
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
            await ProvisionKredarDvaAsync(group);
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

            scheduledDate = group.Frequency switch
            {
                ContributionFrequency.Weekly => scheduledDate.AddDays(7),
                ContributionFrequency.BiWeekly => scheduledDate.AddDays(14),
                _ => scheduledDate.AddMonths(1)
            };
        }

        await payoutRepo.AddRangeAsync(payouts);
    }

    private async Task<int> GetNextPayoutPositionAsync(Guid groupId)
    {
        var payouts = await payoutRepo.GetByGroupAsync(groupId);
        var nextPayout = payouts.FirstOrDefault(p => p.Status == PayoutStatus.Scheduled);
        return nextPayout?.CycleNumber ?? 0;
    }

    private async Task<GroupResponse> MapToResponseAsync(SavingsGroup group)
    {
        var memberCount = (await groupRepo.GetMembersAsync(group.Id)).Count;
        return await MapToResponseBaseAsync(group, memberCount);
    }

    private Task<GroupResponse> MapToResponseBaseAsync(SavingsGroup group, int memberCount) =>
        Task.FromResult(new GroupResponse
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            PrimaryPurpose = group.PrimaryPurpose,
            ContributionAmount = group.ContributionAmount,
            TotalPool = group.ContributionAmount * group.MaxMembers,
            Frequency = group.Frequency.ToString(),
            MaxMembers = group.MaxMembers,
            CurrentMembers = memberCount,
            CreatedByUserId = group.CreatedByUserId,
            Status = group.Status.ToString(),
            StartDate = group.StartDate,
            CreatedAt = group.CreatedAt,
            InviteCode = group.InviteCode,
            InviteLink = BuildInviteLink(group.InviteCode),
            DvaAccountNumber = group.DvaAccountNumber,
            DvaBankName = group.DvaBankName,
            DvaAccountName = group.DvaAccountName,
        });

    private string BuildInviteLink(string inviteCode) =>
        $"{_baseUrl}/join/{inviteCode}";

    public async Task<GroupResponse> UpdateSettingsAsync(Guid userId, Guid groupId, UpdateGroupSettingsRequest request)
    {
        var group = await groupRepo.FindByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Savings group not found.");

        var member = await groupRepo.FindMemberAsync(groupId, userId)
            ?? throw new UnauthorizedAccessException("You are not a member of this group.");
        if (member.Role != GroupMemberRole.Admin)
            throw new UnauthorizedAccessException("Only admins can update group settings.");

        if (!string.IsNullOrWhiteSpace(request.Name)) group.Name = request.Name.Trim();
        if (request.Description != null) group.Description = request.Description;
        if (request.PrimaryPurpose != null) group.PrimaryPurpose = request.PrimaryPurpose;
        await groupRepo.UpdateAsync(group);

        return await MapToResponseAsync(group);
    }

    public async Task<List<GroupMemberDetailResponse>> GetMembersAsync(Guid groupId)
    {
        var members = await groupRepo.GetMembersAsync(groupId);
        var users = await userRepo.FindByIdsAsync(members.Select(m => m.UserId));
        var userLookup = users.ToDictionary(u => u.Id);

        return members.Select(m => new GroupMemberDetailResponse
        {
            MemberId = m.Id,
            UserId = m.UserId,
            FullName = userLookup.TryGetValue(m.UserId, out var u) ? u.FullName : "Unknown",
            Email = userLookup.TryGetValue(m.UserId, out u) ? u.Email : "",
            PayoutPosition = m.PayoutPosition,
            Role = m.Role.ToString(),
            JoinedAt = m.JoinedAt
        }).ToList();
    }

    public async Task RemoveMemberAsync(Guid userId, Guid groupId, Guid memberId)
    {
        var group = await groupRepo.FindByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Savings group not found.");

        var actor = await groupRepo.FindMemberAsync(groupId, userId)
            ?? throw new UnauthorizedAccessException("You are not a member of this group.");
        if (actor.Role != GroupMemberRole.Admin)
            throw new UnauthorizedAccessException("Only admins can remove members.");

        if (group.Status != GroupStatus.Open)
            throw new InvalidOperationException("Members can only be removed from open groups.");

        var target = await db.GroupMembers.FindAsync(memberId)
            ?? throw new KeyNotFoundException("Member not found.");

        if (target.GroupId != groupId)
            throw new KeyNotFoundException("Member not found in this group.");

        if (target.UserId == group.CreatedByUserId)
            throw new InvalidOperationException("Cannot remove the group creator.");

        await groupRepo.DeleteMemberAsync(target);
    }

    public async Task<GroupMemberDetailResponse> UpdateMemberRoleAsync(Guid userId, Guid groupId, Guid memberId, string role)
    {
        if (!Enum.TryParse<GroupMemberRole>(role, true, out var newRole))
            throw new InvalidOperationException("Role must be 'Admin' or 'Member'.");

        var group = await groupRepo.FindByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Savings group not found.");

        var actor = await groupRepo.FindMemberAsync(groupId, userId)
            ?? throw new UnauthorizedAccessException("You are not a member of this group.");
        if (actor.Role != GroupMemberRole.Admin)
            throw new UnauthorizedAccessException("Only admins can change member roles.");

        var target = await db.GroupMembers.FindAsync(memberId)
            ?? throw new KeyNotFoundException("Member not found.");

        if (target.GroupId != groupId)
            throw new KeyNotFoundException("Member not found in this group.");

        target.Role = newRole;
        await groupRepo.UpdateMemberAsync(target);

        var targetUser = await userRepo.FindByIdAsync(target.UserId);
        return new GroupMemberDetailResponse
        {
            MemberId = target.Id,
            UserId = target.UserId,
            FullName = targetUser?.FullName ?? "Unknown",
            Email = targetUser?.Email ?? "",
            PayoutPosition = target.PayoutPosition,
            Role = target.Role.ToString(),
            JoinedAt = target.JoinedAt
        };
    }

    private async Task ProvisionKredarDvaAsync(SavingsGroup group)
    {
        try
        {
            var email = group.ContactEmail ?? $"group-{group.Id:N}@ajovault.app";
            var phone = group.ContactPhone;
            var nameParts = group.Name.Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : "Group";

            var customer = await kredarClient.CreateOrGetCustomerAsync(firstName, lastName, email, phone);
            if (customer == null)
            {
                logger.LogWarning("Kredar customer creation returned null for group {GroupId}", group.Id);
                return;
            }

            var totalPot = group.ContributionAmount * group.MaxMembers;
            var dva = await kredarClient.CreateOrGetDvaAsync(customer.Id, totalPot);
            if (dva == null)
            {
                logger.LogWarning("Kredar DVA creation returned null for group {GroupId}", group.Id);
                return;
            }

            group.KredarCustomerId = customer.Id;
            group.KredarDvaId = dva.Id;
            group.DvaAccountNumber = dva.AccountNumber;
            group.DvaBankName = dva.BankName;
            group.DvaAccountName = dva.AccountName;
            await groupRepo.UpdateAsync(group);

            logger.LogInformation("Kredar DVA {AccountNumber} provisioned for group {GroupId}", dva.AccountNumber, group.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to provision Kredar DVA for group {GroupId}", group.Id);
        }
    }

    private static string GenerateInviteCode(string groupName)
    {
        var slug = new string(groupName.ToLower()
            .Where(c => char.IsLetterOrDigit(c) || c == ' ')
            .ToArray())
            .Replace(' ', '-')
            .Trim('-');

        if (slug.Length > 20) slug = slug[..20].TrimEnd('-');

        var suffix = Guid.NewGuid().ToString("N")[..4];
        return $"{slug}-{suffix}";
    }

    private static int GetCurrentCycleNumber(SavingsGroup group)
    {
        if (group.Status != GroupStatus.Active || !group.StartDate.HasValue)
            return 0;

        var daysSinceStart = (DateTime.UtcNow - group.StartDate.Value).Days;
        var cycleDays = group.Frequency switch
        {
            ContributionFrequency.Weekly => 7,
            ContributionFrequency.BiWeekly => 14,
            _ => 30
        };

        return Math.Max(1, (daysSinceStart / cycleDays) + 1);
    }

    private static bool IsOverdue(SavingsGroup group, int cycleNumber)
    {
        if (!group.StartDate.HasValue) return false;

        var cycleDays = group.Frequency switch
        {
            ContributionFrequency.Weekly => 7,
            ContributionFrequency.BiWeekly => 14,
            _ => 30
        };

        var cycleEnd = group.StartDate.Value.AddDays(cycleDays * cycleNumber);
        return DateTime.UtcNow > cycleEnd;
    }
}
