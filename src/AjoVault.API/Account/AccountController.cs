using AjoVault.API.Account.Dto;
using AjoVault.API.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjoVault.API.Account;

[ApiController]
[Route("api/v1/account")]
[Authorize]
public class AccountController(AccountService accountService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = UserContext.GetUserId(HttpContext);
        var account = await accountService.GetAsync(userId);
        return Ok(ApiResponse<AccountResponse>.Success(account));
    }

    [HttpPatch]
    public async Task<IActionResult> Update([FromBody] UpdateAccountRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var account = await accountService.UpdateAsync(userId, request);
        return Ok(ApiResponse<AccountResponse>.Success(account, "Profile updated."));
    }

    [HttpPatch("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);
        await accountService.ChangePasswordAsync(userId, request);
        return Ok(ApiResponse<object>.Success(new { }, "Password changed successfully."));
    }

    [HttpPatch("pin")]
    public async Task<IActionResult> ChangePin([FromBody] ChangePinRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);
        await accountService.ChangePinAsync(userId, request);
        return Ok(ApiResponse<object>.Success(new { }, "PIN updated successfully."));
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions()
    {
        var userId = UserContext.GetUserId(HttpContext);
        var transactions = await accountService.GetTransactionsAsync(userId);
        return Ok(ApiResponse<List<TransactionResponse>>.Success(transactions));
    }
}
