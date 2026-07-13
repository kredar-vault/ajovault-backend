namespace AjoVault.API.Payouts.Dto;

public class RotationMemberResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Initials { get; set; }
    public int Position { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "WAITING"; // PAID | CURRENT | WAITING
    public string? DateInfo { get; set; }
}
