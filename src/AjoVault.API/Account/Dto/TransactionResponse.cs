namespace AjoVault.API.Account.Dto;

public class TransactionResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Direction { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
