namespace AjoVault.API.Payouts.Dto;

public class PayoutSummaryResponse
{
    public string CurrentCycle { get; set; } = "N/A";
    public string CurrentRecipientName { get; set; } = "None";
    public string NextRecipientName { get; set; } = "None";
    public int TotalMembers { get; set; }
    public int PayoutsDone { get; set; }
}
