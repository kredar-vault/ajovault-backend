namespace AjoVault.API.Contributions;

public enum ContributionStatus
{
    Received,
    Pending,
    Missed
}

public class Contribution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public int CycleNumber { get; set; }
    public decimal Amount { get; set; }
    public ContributionStatus Status { get; set; } = ContributionStatus.Received;
    public string Reference { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}
