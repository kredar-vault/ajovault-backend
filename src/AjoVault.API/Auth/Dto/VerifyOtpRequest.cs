using System.ComponentModel.DataAnnotations;

namespace AjoVault.API.Auth.Dto;

public class VerifyOtpRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Otp { get; set; } = string.Empty;
}
