using System.ComponentModel.DataAnnotations;

namespace AjoVault.API.Auth.Dto;

public class VerifyLoginOtpRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Otp { get; set; } = string.Empty;
}

public class ResendLoginOtpRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
