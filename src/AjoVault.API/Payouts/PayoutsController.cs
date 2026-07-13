using AjoVault.API.Common;
using AjoVault.API.Payouts.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjoVault.API.Payouts;

[ApiController]
[Authorize]
public class PayoutsController(PayoutsService payoutsService) : ControllerBase
{
    [HttpGet("api/v1/payouts/upcoming")]
    public async Task<IActionResult> GetUpcoming()
    {
        var userId = UserContext.GetUserId(HttpContext);
        var payouts = await payoutsService.GetUpcomingByUserAsync(userId);
        return Ok(ApiResponse<List<PayoutResponse>>.Success(payouts));
    }

    [HttpGet("api/v1/groups/{groupId:guid}/payouts")]
    public async Task<IActionResult> GetByGroup(Guid groupId)
    {
        var payouts = await payoutsService.GetByGroupAsync(groupId);
        return Ok(ApiResponse<List<RotationMemberResponse>>.Success(payouts));
    }

    [HttpGet("api/v1/groups/{groupId:guid}/payouts/current")]
    public async Task<IActionResult> GetCurrentPayout(Guid groupId)
    {
        var summary = await payoutsService.GetCurrentPayoutAsync(groupId);
        return Ok(ApiResponse<PayoutSummaryResponse>.Success(summary));
    }

    [HttpGet("api/v1/groups/{groupId:guid}/payouts/upcoming")]
    public async Task<IActionResult> GetUpcomingByGroup(Guid groupId)
    {
        var payouts = await payoutsService.GetUpcomingByGroupAsync(groupId);
        return Ok(ApiResponse<List<PayoutResponse>>.Success(payouts));
    }

    [HttpPost("api/v1/groups/{groupId:guid}/payouts/{payoutId:guid}/disburse")]
    public async Task<IActionResult> Disburse(Guid groupId, Guid payoutId)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var payout = await payoutsService.DisburseAsync(userId, groupId, payoutId);
        return Ok(ApiResponse<PayoutResponse>.Success(payout, "Payout disbursed successfully."));
    }
}
