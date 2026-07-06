using System.ComponentModel.DataAnnotations;

namespace AjoVault.API.Auth.Dto;

public class RegisterRequest
{
    [Required]
    [RegularExpression(@"^[a-zA-Z\s'\-]+$", ErrorMessage = "Name can only contain letters, spaces, hyphens, and apostrophes.")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}
