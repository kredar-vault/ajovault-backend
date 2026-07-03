namespace AjoVault.API.Contributions.Dto;

public class ContributionResponse
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public int CycleNumber { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
}
