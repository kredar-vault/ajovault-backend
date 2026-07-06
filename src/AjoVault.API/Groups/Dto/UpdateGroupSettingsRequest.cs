namespace AjoVault.API.Groups.Dto;

public class UpdateGroupSettingsRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? PrimaryPurpose { get; set; }
}

public class UpdateMemberRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class GroupMemberDetailResponse
{
    public Guid MemberId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int PayoutPosition { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}
