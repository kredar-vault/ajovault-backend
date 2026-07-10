using System.ComponentModel.DataAnnotations;

namespace AjoVault.API.Groups.Dto;

public class CreateGroupRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? PrimaryPurpose { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal ContributionAmount { get; set; }

    [Required]
    public string Frequency { get; set; } = "Monthly";

    [Range(2, 100)]
    public int MaxMembers { get; set; }

    [Required, EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;

    [Required]
    public string ContactPhone { get; set; } = string.Empty;
}
