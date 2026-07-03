namespace AjoVault.API.Dashboard.Dto;

public class DashboardResponse
{
    public decimal TotalContributions { get; set; }
    public int TotalMembers { get; set; }
    public int PendingContributionsCount { get; set; }
    public int UpcomingPayoutsCount { get; set; }
    public ContributionProgress ContributionProgress { get; set; } = new();
    public NextPayout? NextPayout { get; set; }
}

public class ContributionProgress
{
    public int Received { get; set; }
    public int Pending { get; set; }
    public int Missed { get; set; }
    public int Total { get; set; }
}

public class NextPayout
{
    public Guid PayoutId { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ScheduledDate { get; set; }
    public int DaysUntil { get; set; }
}
