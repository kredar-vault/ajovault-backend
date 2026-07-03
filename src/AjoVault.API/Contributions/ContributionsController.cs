using AjoVault.API.Common;
using AjoVault.API.Contributions.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjoVault.API.Contributions;

[ApiController]
[Authorize]
public class ContributionsController(ContributionsService contributionsService) : ControllerBase
{
    [HttpGet("api/v1/contributions")]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(HttpContext);
        var contributions = await contributionsService.GetAllByUserAsync(userId);
        return Ok(ApiResponse<List<ContributionResponse>>.Success(contributions));
    }

    [HttpGet("api/v1/groups/{groupId:guid}/contributions")]
    public async Task<IActionResult> GetByGroup(Guid groupId)
    {
        var contributions = await contributionsService.GetByGroupAsync(groupId);
        return Ok(ApiResponse<List<ContributionResponse>>.Success(contributions));
    }

    [HttpGet("api/v1/groups/{groupId:guid}/contributions/mine")]
    public async Task<IActionResult> GetMine(Guid groupId)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var contributions = await contributionsService.GetMineAsync(groupId, userId);
        return Ok(ApiResponse<List<ContributionResponse>>.Success(contributions));
    }

    [HttpPost("api/v1/groups/{groupId:guid}/contributions")]
    public async Task<IActionResult> Record(Guid groupId, [FromBody] RecordContributionRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var contribution = await contributionsService.RecordAsync(userId, groupId, request);
        return Ok(ApiResponse<ContributionResponse>.Success(contribution, "Contribution recorded successfully."));
    }
}
