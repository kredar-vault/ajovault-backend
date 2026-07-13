namespace AjoVault.API.Dashboard.Dto;

public class GroupDashboardResponse
{
    public GroupDashboardStats Stats { get; set; } = new();
    public GroupContributionProgress Progress { get; set; } = new();
    public GroupNextPayout Payout { get; set; } = new();
    public List<GroupActivityRow> Activity { get; set; } = [];
}

public class GroupDashboardStats
{
    public decimal TotalContribution { get; set; }
    public int TotalMembers { get; set; }
    public int PendingContributions { get; set; }
    public int UpcomingPayouts { get; set; }
}

public class GroupContributionProgress
{
    public string Month { get; set; } = string.Empty;
    public int ReceivedCount { get; set; }
    public int PendingCount { get; set; }
    public int MissedCount { get; set; }
    public int TotalCount { get; set; }
}

public class GroupNextPayout
{
    public string RecipientName { get; set; } = "None";
    public string? AvatarUrl { get; set; }
    public decimal Amount { get; set; }
    public int DaysRemaining { get; set; }
}

public class GroupActivityRow
{
    public string Id { get; set; } = string.Empty;
    public string Member { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}
