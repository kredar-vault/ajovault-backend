using System.ComponentModel.DataAnnotations;

namespace AjoVault.API.Auth.Dto;

public class ResendOtpRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
