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

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions()
    {
        var userId = UserContext.GetUserId(HttpContext);
        var transactions = await accountService.GetTransactionsAsync(userId);
        return Ok(ApiResponse<List<TransactionResponse>>.Success(transactions));
    }
}
