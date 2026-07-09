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

    [HttpGet("api/v1/wallet/virtual-account")]
    [HttpGet("api/v1/wallets/virtual-account")]
    public async Task<IActionResult> GetVirtualAccount()
    {
        var userId = UserContext.GetUserId(HttpContext);
        var result = await walletService.GetVirtualAccountAsync(userId);
        return Ok(ApiResponse<VirtualAccountResponse>.Success(result));
    }

    [HttpPost("api/v1/wallets/create-virtual-account")]
    public async Task<IActionResult> CreateVirtualAccount([FromBody] CreateVirtualAccountRequest request)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var result = await walletService.SetBankAccountAsync(userId, request);
        return Ok(ApiResponse<VirtualAccountResponse>.Success(result, "Bank account saved."));
    }
}
