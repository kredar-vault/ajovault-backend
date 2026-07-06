namespace AjoVault.API.Groups;

public enum ContributionFrequency
{
    Weekly,
    BiWeekly,
    Monthly
}

public enum GroupStatus
{
    Open,
    Active,
    Completed
}

public class SavingsGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PrimaryPurpose { get; set; }
    public decimal ContributionAmount { get; set; }
    public ContributionFrequency Frequency { get; set; }
    public int MaxMembers { get; set; }
    public Guid CreatedByUserId { get; set; }
    public GroupStatus Status { get; set; } = GroupStatus.Open;
    public DateTime? StartDate { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Kredar DVA fields — populated when group becomes Active
    public Guid? KredarCustomerId { get; set; }
    public Guid? KredarDvaId { get; set; }
    public string? DvaAccountNumber { get; set; }
    public string? DvaBankName { get; set; }
    public string? DvaAccountName { get; set; }
}
