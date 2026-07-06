namespace AjoVault.API.Groups.Dto;

public class GroupResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PrimaryPurpose { get; set; }
    public decimal ContributionAmount { get; set; }
    public decimal TotalPool { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public int MaxMembers { get; set; }
    public int CurrentMembers { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public string InviteLink { get; set; } = string.Empty;
    public string? DvaAccountNumber { get; set; }
    public string? DvaBankName { get; set; }
    public string? DvaAccountName { get; set; }
}
