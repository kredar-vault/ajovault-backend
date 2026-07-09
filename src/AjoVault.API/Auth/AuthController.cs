using AjoVault.API.Auth.Dto;
using AjoVault.API.Common;
using Microsoft.AspNetCore.Mvc;

namespace AjoVault.API.Auth;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return Ok(ApiResponse<RegisterResponse>.Success(result, result.Message));
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var result = await authService.VerifyOtpAsync(request);
        return Ok(ApiResponse<AuthResponse>.Success(result, "Email verified. Welcome to AjoVault!"));
    }

    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
    {
        await authService.ResendOtpAsync(request.Email);
        return Ok(ApiResponse<object>.Success(new { }, "A new OTP has been sent to your email."));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponse>.Success(result, "Login successful."));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await authService.ForgotPasswordAsync(request.Email);
        return Ok(ApiResponse<object>.Success(new { }, "If this email is registered, a reset link has been sent."));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await authService.ResetPasswordAsync(request);
        return Ok(ApiResponse<object>.Success(new { }, "Password reset successfully."));
    }
}
