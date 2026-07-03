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
        return Ok(ApiResponse<GroupResponse>.Success(group, "Savings group created successfully."));
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(Guid id)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var group = await groupsService.JoinAsync(userId, id);
        return Ok(ApiResponse<GroupResponse>.Success(group, "Joined savings group successfully."));
    }
}
