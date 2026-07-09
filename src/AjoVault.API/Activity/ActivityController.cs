using AjoVault.API.Auth;
using AjoVault.API.Common;
using AjoVault.API.Contributions;
using AjoVault.API.Groups;
using AjoVault.API.Payouts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjoVault.API.Activity;

public record ActivityItem(string Type, string Description, Guid? UserId, decimal? Amount, int? CycleNumber, string? Reference, DateTime OccurredAt);

[ApiController]
[Authorize]
public class ActivityController(
    GroupRepository groupRepo,
    ContributionRepository contributionRepo,
    PayoutRepository payoutRepo,
    UserRepository userRepo) : ControllerBase
{
    [HttpGet("api/v1/groups/{groupId:guid}/activity")]
    public async Task<IActionResult> GetGroupActivity(Guid groupId)
    {
        var members = await groupRepo.GetMembersAsync(groupId);
        var userIds = members.Select(m => m.UserId).Distinct();
        var users = await userRepo.FindByIdsAsync(userIds);
        var userLookup = users.ToDictionary(u => u.Id, u => u.FullName);

        var contributions = await contributionRepo.GetByGroupAsync(groupId);
        var payouts = await payoutRepo.GetByGroupAsync(groupId);

        var activities = new List<ActivityItem>();

        foreach (var m in members)
            activities.Add(new ActivityItem("member_joined",
                $"{userLookup.GetValueOrDefault(m.UserId, "Someone")} joined the group",
                m.UserId, null, null, null, m.JoinedAt));

        foreach (var c in contributions)
            activities.Add(new ActivityItem("contribution",
                $"{userLookup.GetValueOrDefault(c.UserId, "Someone")} contributed ₦{c.Amount:N0} (Cycle {c.CycleNumber})",
                c.UserId, c.Amount, c.CycleNumber, c.Reference, c.PaidAt));

        foreach (var p in payouts.Where(p => p.Status == PayoutStatus.Disbursed))
            activities.Add(new ActivityItem("payout_disbursed",
                $"₦{p.Amount:N0} disbursed to {userLookup.GetValueOrDefault(p.RecipientUserId, "a member")} (Cycle {p.CycleNumber})",
                p.RecipientUserId, p.Amount, p.CycleNumber, null, p.DisbursedAt ?? p.ScheduledDate));

        return Ok(ApiResponse<List<ActivityItem>>.Success(
            activities.OrderByDescending(a => a.OccurredAt).Take(50).ToList()));
    }
}
