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
    // Kredar-provisioned personal DVA
    public Guid? KredarCustomerId { get; set; }
    public string? DvaAccountNumber { get; set; }
    public string? DvaBankName { get; set; }
    public string? DvaAccountName { get; set; }
    public bool IsVerified { get; set; } = false;
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
