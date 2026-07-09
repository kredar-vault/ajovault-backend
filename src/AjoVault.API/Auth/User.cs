namespace AjoVault.API.Auth;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? PinHash { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountName { get; set; }
    public string? BankCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
