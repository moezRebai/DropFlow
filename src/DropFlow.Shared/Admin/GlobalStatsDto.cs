namespace DropFlow.Shared.Admin;


public class GlobalStatsDto(
    int totalTenants,
    int activeTenants,
    int inactiveTenants,
    int totalUsers,
    int activeUsers,
    int tenantsCreatedThisMonth,
    int tenantsCreatedThisWeek,
    int usersCreatedThisMonth,
    Dictionary<string, int> tenantsByPlan)
{
    public GlobalStatsDto() : this(0, 0, 0, 0, 0, 
        0, 0, 0, new Dictionary<string, int>())
    {
    }

    public int TotalTenants { get; set; } = totalTenants;
    public int ActiveTenants { get; set; } = activeTenants;
    public int InactiveTenants { get; set; } = inactiveTenants;
    public int TotalUsers { get; set; } = totalUsers;
    public int ActiveUsers { get; set; } = activeUsers;
    public int TenantsCreatedThisMonth { get; set; } = tenantsCreatedThisMonth;
    public int TenantsCreatedThisWeek { get; set; } = tenantsCreatedThisWeek;
    public int UsersCreatedThisMonth { get; set; } = usersCreatedThisMonth;
    public Dictionary<string, int> TenantsByPlan { get; set; } = tenantsByPlan;
}
