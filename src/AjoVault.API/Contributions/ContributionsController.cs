using AjoVault.API.Common;
using AjoVault.API.Contributions.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjoVault.API.Contributions;

[ApiController]
[Route("api/v1/groups/{groupId:guid}/contributions")]
[Authorize]
public class ContributionsController(ContributionsService contributionsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid groupId)
    {
        var contributions = await contributionsService.GetByGroupAsync(groupId);
        return Ok(ApiResponse<List<ContributionResponse>>.Success(contributions));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(Guid groupId)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var contributions = await contributionsService.GetMineAsync(groupId, userId);
        return Ok(ApiResponse<List<ContributionResponse>>.Success(contributions));
    }

    [HttpPost]
    public async Task<IActionResult> Record(Guid groupId, [FromBody] RecordContributionRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var contribution = await contributionsService.RecordAsync(userId, groupId, request);
        return Ok(ApiResponse<ContributionResponse>.Success(contribution, "Contribution recorded successfully."));
    }
}
