namespace AjoVault.API.Groups.Dto;

public class GroupMemberResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int PayoutPosition { get; set; }
    public DateTime JoinedAt { get; set; }
}
