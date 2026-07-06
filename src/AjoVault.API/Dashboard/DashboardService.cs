using AjoVault.API.Auth;
using AjoVault.API.Contributions;
using AjoVault.API.Dashboard.Dto;
using AjoVault.API.Groups;
using AjoVault.API.Payouts;

namespace AjoVault.API.Dashboard;

public class DashboardService(
    GroupRepository groupRepo,
    ContributionRepository contributionRepo,
    PayoutRepository payoutRepo,
    UserRepository userRepo)
{
    public async Task<DashboardResponse> GetAsync(Guid userId)
    {
        var myGroups = await groupRepo.GetByMemberAsync(userId);
        var groupIds = myGroups.Select(g => g.Id).ToList();

        var allMembers = await Task.WhenAll(groupIds.Select(id => groupRepo.GetMembersAsync(id)));
        var totalMembers = allMembers.Sum(m => m.Count);

        var contributions = await contributionRepo.GetAllByUserGroupsAsync(groupIds);
        var totalContributions = contributions
            .Where(c => c.Status == ContributionStatus.Received)
            .Sum(c => c.Amount);

        var payouts = await payoutRepo.GetByGroupIdsAsync(groupIds);
        var upcomingPayouts = payouts.Where(p => p.Status == PayoutStatus.Scheduled).ToList();
        var nextPayout = upcomingPayouts.OrderBy(p => p.ScheduledDate).FirstOrDefault();

        var activeGroups = myGroups.Where(g => g.Status == GroupStatus.Active).ToList();
        var received = 0;
        var missed = 0;
        var pending = 0;
        var total = 0;

        foreach (var group in activeGroups)
        {
            var groupMembers = allMembers.FirstOrDefault(m => m.FirstOrDefault()?.GroupId == group.Id) ?? [];
            var currentCycle = GetCurrentCycle(group);
            if (currentCycle <= 0) continue;

            var cycleContribs = contributions.Where(c => c.GroupId == group.Id && c.CycleNumber == currentCycle).ToList();
            var paidUserIds = cycleContribs.Select(c => c.UserId).ToHashSet();
            var isOverdue = IsOverdue(group, currentCycle);

            foreach (var member in groupMembers)
            {
                total++;
                if (paidUserIds.Contains(member.UserId)) received++;
                else if (isOverdue) missed++;
                else pending++;
            }
        }

        NextPayout? nextPayoutDto = null;
        if (nextPayout != null)
        {
            var group = myGroups.FirstOrDefault(g => g.Id == nextPayout.GroupId);
            var recipient = await userRepo.FindByIdAsync(nextPayout.RecipientUserId);
            nextPayoutDto = new NextPayout
            {
                PayoutId = nextPayout.Id,
                GroupId = nextPayout.GroupId,
                GroupName = group?.Name ?? "",
                RecipientName = recipient?.FullName ?? "Unknown",
                Amount = nextPayout.Amount,
                ScheduledDate = nextPayout.ScheduledDate,
                DaysUntil = Math.Max(0, (nextPayout.ScheduledDate - DateTime.UtcNow).Days)
            };
        }

        return new DashboardResponse
        {
            TotalContributions = totalContributions,
            TotalMembers = totalMembers,
            PendingContributionsCount = pending + missed,
            UpcomingPayoutsCount = upcomingPayouts.Count,
            ContributionProgress = new ContributionProgress
            {
                Received = received,
                Pending = pending,
                Missed = missed,
                Total = total
            },
            NextPayout = nextPayoutDto
        };
    }

    public async Task<object> GetByGroupAsync(Guid userId, Guid groupId)
    {
        var group = await groupRepo.FindByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Savings group not found.");

        var members = await groupRepo.GetMembersAsync(groupId);
        var contributions = await contributionRepo.GetByGroupAsync(groupId);
        var payouts = await payoutRepo.GetByGroupAsync(groupId);

        var currentCycle = GetCurrentCycle(group);
        var cycleContribs = contributions.Where(c => c.CycleNumber == currentCycle).ToList();
        var paidCount = cycleContribs.Count;
        var totalCollected = cycleContribs.Sum(c => c.Amount);

        var myMember = members.FirstOrDefault(m => m.UserId == userId);
        var myContribThisCycle = cycleContribs.Any(c => c.UserId == userId);

        var nextPayout = payouts.Where(p => p.Status == PayoutStatus.Scheduled).OrderBy(p => p.CycleNumber).FirstOrDefault();
        User? nextRecipient = nextPayout != null ? await userRepo.FindByIdAsync(nextPayout.RecipientUserId) : null;

        return new
        {
            groupId = group.Id,
            groupName = group.Name,
            status = group.Status.ToString(),
            currentCycle,
            totalMembers = members.Count,
            paidThisCycle = paidCount,
            pendingThisCycle = members.Count - paidCount,
            totalCollectedThisCycle = totalCollected,
            myContributionStatus = myMember == null ? "not_member"
                : myContribThisCycle ? "paid" : "pending",
            nextPayout = nextPayout == null ? null : new
            {
                cycleNumber = nextPayout.CycleNumber,
                recipient = nextRecipient?.FullName ?? "Unknown",
                amount = nextPayout.Amount,
                scheduledDate = nextPayout.ScheduledDate
            },
            dvaAccountNumber = group.DvaAccountNumber,
            dvaBankName = group.DvaBankName,
            dvaAccountName = group.DvaAccountName
        };
    }

    private static int GetCurrentCycle(SavingsGroup group)
    {
        if (!group.StartDate.HasValue) return 0;
        var days = (DateTime.UtcNow - group.StartDate.Value).Days;
        var cycleDays = group.Frequency switch
        {
            ContributionFrequency.Weekly => 7,
            ContributionFrequency.BiWeekly => 14,
            _ => 30
        };
        return Math.Max(1, (days / cycleDays) + 1);
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
        return DateTime.UtcNow > group.StartDate.Value.AddDays(cycleDays * cycleNumber);
    }
}
