using System.ComponentModel.DataAnnotations;

namespace AjoVault.API.Account.Dto;

public class UpdateAccountRequest
{
    [RegularExpression(@"^[a-zA-Z\s'\-]+$", ErrorMessage = "Name can only contain letters, spaces, hyphens, and apostrophes.")]
    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePinRequest
{
    public string? CurrentPin { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 4, ErrorMessage = "PIN must be 4–6 digits.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "PIN must be digits only.")]
    public string NewPin { get; set; } = string.Empty;
}
