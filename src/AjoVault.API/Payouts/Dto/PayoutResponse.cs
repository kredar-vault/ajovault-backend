namespace AjoVault.API.Payouts.Dto;

public class PayoutResponse
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int CycleNumber { get; set; }
    public Guid RecipientUserId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? DisbursedAt { get; set; }
    public int DaysUntil { get; set; }
}
