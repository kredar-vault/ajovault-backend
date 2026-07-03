using AjoVault.API.Common;
using AjoVault.API.Payouts.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjoVault.API.Payouts;

[ApiController]
[Route("api/v1/groups/{groupId:guid}/payouts")]
[Authorize]
public class PayoutsController(PayoutsService payoutsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid groupId)
    {
        var payouts = await payoutsService.GetByGroupAsync(groupId);
        return Ok(ApiResponse<List<PayoutResponse>>.Success(payouts));
    }

    [HttpPost("{payoutId:guid}/disburse")]
    public async Task<IActionResult> Disburse(Guid groupId, Guid payoutId)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var payout = await payoutsService.DisburseAsync(userId, groupId, payoutId);
        return Ok(ApiResponse<PayoutResponse>.Success(payout, "Payout disbursed successfully."));
    }
}
