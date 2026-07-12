namespace AjoVault.API.Wallet;

public class Deposit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Source { get; set; } = "DVA";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
