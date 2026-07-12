namespace AjoVault.API.Contributions.Dto;

public class RecordContributionRequest
{
    public int? CycleNumber { get; set; } // null = auto-detect from current scheduled payout
}
