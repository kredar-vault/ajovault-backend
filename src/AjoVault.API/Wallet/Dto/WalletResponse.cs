namespace AjoVault.API.Wallet.Dto;

public class WalletResponse
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public string Currency { get; set; } = "NGN";
    public int ActiveGroups { get; set; }
    public int TotalGroups { get; set; }
    public VirtualAccountResponse? VirtualAccount { get; set; }
}

public class VirtualAccountResponse
{
    public string? AccountNumber { get; set; }
    public string? AccountName { get; set; }
    public string? BankCode { get; set; }
    public bool IsSet { get; set; }
}

public class CreateVirtualAccountRequest
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
}
