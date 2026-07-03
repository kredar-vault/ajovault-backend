namespace AjoVault.API.Groups;

public class GroupMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public int PayoutPosition { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
