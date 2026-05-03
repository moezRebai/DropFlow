using DropFlow.Domain.Enums;

namespace DropFlow.Shared.Routes;

public class RouteTeamDto
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public string DriverName { get; set; }
    public TeamMemberRole Role { get; set; }
    public string RoleDisplay { get; set; }
}