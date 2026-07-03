namespace AjoVault.API.Groups.Dto;

public class GroupDetailResponse : GroupResponse
{
    public List<GroupMemberResponse> Members { get; set; } = [];
}
