namespace DropFlow.Shared.Admin;

public class UserStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int UsersCreatedThisMonth { get; set; }
    public int UsersCreatedThisWeek { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = new();
    public Dictionary<int, int> UsersByTenant { get; set; } = new();
}