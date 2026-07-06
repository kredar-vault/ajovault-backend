using System.ComponentModel.DataAnnotations;

namespace AjoVault.API.Auth.Dto;

public class ResetPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}
