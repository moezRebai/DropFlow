using DropFlow.Shared.Enums;

namespace DropFlow.Domain.Entities;

public class RouteTeam
{
    public int Id { get; private set; }
    public int TenantId { get;  set; }
    public int RouteId { get; private set; }
    public Route Route { get; set; }
    
    public int DriverId { get; private set; }
    public Driver Driver { get; set; }
    
    public TeamMemberRole Role { get; private set; }
    
    public DateTime AssignedDate { get; private set; }
    
    private RouteTeam() { }
    
    public static RouteTeam Create(
        int routeId,
        int driverId,
        TeamMemberRole role)
    {
        return new RouteTeam
        {
            RouteId = routeId,
            DriverId = driverId,
            Role = role,
            AssignedDate = DateTime.UtcNow
        };
    }
    
    public void UpdateRole(TeamMemberRole role)
    {
        Role = role;
    }
}