namespace AjoVault.API.Account.Dto;

public class AccountResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string MaskedAccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public MonthlySummary MonthlySummary { get; set; } = new();
}

public class MonthlySummary
{
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public List<MonthlyPoint> Points { get; set; } = [];
}

public class MonthlyPoint
{
    public string Month { get; set; } = string.Empty;
    public decimal In { get; set; }
    public decimal Out { get; set; }
}
