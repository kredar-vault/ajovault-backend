using AjoVault.API.Common;
using AjoVault.API.Wallet.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjoVault.API.Wallet;

[ApiController]
[Authorize]
public class WalletController(WalletService walletService) : ControllerBase
{
    [HttpGet("api/v1/wallet")]
    public async Task<IActionResult> GetWallet()
    {
        var userId = UserContext.GetUserId(HttpContext);
        var result = await walletService.GetWalletAsync(userId);
        return Ok(ApiResponse<WalletResponse>.Success(result));
    }

    [HttpGet("api/v1/wallet/balance")]
    public async Task<IActionResult> GetBalance()
    {
        var userId = UserContext.GetUserId(HttpContext);
        var result = await walletService.GetWalletAsync(userId);
        return Ok(ApiResponse<object>.Success(new
        {
            balance = result.Balance,
            totalIn = result.TotalIn,
            totalOut = result.TotalOut,
            currency = "NGN"
        }));
    }

    [HttpGet("api/v1/wallet/dva")]
    public async Task<IActionResult> GetVirtualAccount()
    {
        var userId = UserContext.GetUserId(HttpContext);
        var result = await walletService.GetVirtualAccountAsync(userId);
        return Ok(ApiResponse<VirtualAccountResponse>.Success(result));
    }

    [HttpPost("api/v1/wallet/payout-account/lookup")]
    public async Task<IActionResult> LookupBank([FromBody] BankLookupRequest request, CancellationToken ct)
    {
        var result = await walletService.LookupBankAsync(request.AccountNumber, request.BankCode, ct);
        return Ok(ApiResponse<BankLookupResponse>.Success(result));
    }

    [HttpPost("api/v1/wallet/payout-account")]
    public async Task<IActionResult> SavePayoutAccount([FromBody] CreateVirtualAccountRequest request, CancellationToken ct)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var result = await walletService.SetBankAccountAsync(userId, request, ct);
        return Ok(ApiResponse<VirtualAccountResponse>.Success(result, "Payout account saved."));
    }

    [HttpPost("api/v1/wallet/withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var result = await walletService.WithdrawAsync(userId, request.Amount);
        return Ok(ApiResponse<WithdrawalResponse>.Success(result, "Withdrawal initiated successfully."));
    }
}
