using AjoVault.API.Common;
using AjoVault.API.Groups.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjoVault.API.Groups;

[ApiController]
[Route("api/v1/groups")]
[Authorize]
public class GroupsController(GroupsService groupsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var groups = await groupsService.GetAllAsync();
        return Ok(ApiResponse<List<GroupResponse>>.Success(groups));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var userId = UserContext.GetUserId(HttpContext);
        var groups = await groupsService.GetMyGroupsAsync(userId);
        return Ok(ApiResponse<List<GroupResponse>>.Success(groups));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var group = await groupsService.GetByIdAsync(id);
        return Ok(ApiResponse<GroupDetailResponse>.Success(group));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var group = await groupsService.CreateAsync(userId, request);
        return Ok(ApiResponse<GroupResponse>.Success(group, "Savings circle created successfully."));
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(Guid id)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var group = await groupsService.JoinAsync(userId, id);
        return Ok(ApiResponse<GroupResponse>.Success(group, "Joined savings circle successfully."));
    }

    [HttpPost("join/{inviteCode}")]
    public async Task<IActionResult> JoinByInviteCode(string inviteCode)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var group = await groupsService.JoinByInviteCodeAsync(userId, inviteCode);
        return Ok(ApiResponse<GroupResponse>.Success(group, "Joined savings circle successfully."));
    }

    [HttpGet("{id:guid}/invite")]
    public async Task<IActionResult> GetInviteLink(Guid id)
    {
        var link = await groupsService.GetInviteLinkAsync(id);
        return Ok(ApiResponse<object>.Success(new { inviteLink = link }));
    }

    [HttpPost("{id:guid}/invite")]
    public async Task<IActionResult> SendInvite(Guid id)
    {
        var link = await groupsService.GetInviteLinkAsync(id);
        return Ok(ApiResponse<object>.Success(new { inviteLink = link, inviteCode = link.Split('/').Last() }, "Invite link generated."));
    }

    [HttpGet("{groupId:guid}/settings")]
    public async Task<IActionResult> GetSettings(Guid groupId)
    {
        var group = await groupsService.GetByIdAsync(groupId);
        return Ok(ApiResponse<object>.Success(new
        {
            group.Id, group.Name, group.Description, group.PrimaryPurpose,
            group.ContributionAmount, group.Frequency, group.MaxMembers, group.Status,
            group.CreatedAt, group.InviteCode, group.CurrentMembers, group.CreatedByUserId,
            group.DvaAccountNumber, group.DvaBankName, group.DvaAccountName
        }));
    }

    [HttpPatch("{groupId:guid}/settings")]
    public async Task<IActionResult> UpdateSettings(Guid groupId, [FromBody] UpdateGroupSettingsRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var group = await groupsService.UpdateSettingsAsync(userId, groupId, request);
        return Ok(ApiResponse<GroupResponse>.Success(group, "Group settings updated."));
    }

    [HttpGet("{groupId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid groupId)
    {
        var members = await groupsService.GetMembersAsync(groupId);
        return Ok(ApiResponse<List<GroupMemberDetailResponse>>.Success(members));
    }

    [HttpDelete("{groupId:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid groupId, Guid memberId)
    {
        var userId = UserContext.GetUserId(HttpContext);
        await groupsService.RemoveMemberAsync(userId, groupId, memberId);
        return Ok(ApiResponse<object>.Success(new { }, "Member removed."));
    }

    [HttpPatch("{groupId:guid}/members/{memberId:guid}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid groupId, Guid memberId, [FromBody] UpdateMemberRoleRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var member = await groupsService.UpdateMemberRoleAsync(userId, groupId, memberId, request.Role);
        return Ok(ApiResponse<GroupMemberDetailResponse>.Success(member, "Member role updated."));
    }

    [HttpDelete("{groupId:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid groupId)
    {
        var userId = UserContext.GetUserId(HttpContext);
        await groupsService.DeleteGroupAsync(userId, groupId);
        return Ok(ApiResponse<object>.Success(new { }, "Circle deleted successfully."));
    }

    [HttpPost("{groupId:guid}/leave")]
    public async Task<IActionResult> LeaveGroup(Guid groupId)
    {
        var userId = UserContext.GetUserId(HttpContext);
        await groupsService.LeaveGroupAsync(userId, groupId);
        return Ok(ApiResponse<object>.Success(new { }, "You have left the circle."));
    }
}
