using DropFlow.Shared.Enums;

namespace DropFlow.Shared.Routes;

public class TeamMemberDto
{
    public int DriverId { get; set; }
    public TeamMemberRole Role { get; set; }
}