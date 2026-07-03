namespace AjoVault.API.Payouts;

public enum PayoutStatus
{
    Scheduled,
    Disbursed
}

public class Payout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public int CycleNumber { get; set; }
    public Guid RecipientUserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ScheduledDate { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Scheduled;
    public DateTime? DisbursedAt { get; set; }
}
