namespace AjoVault.API.Contributions.Dto;

public class ContributionResponse
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int CycleNumber { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
}
