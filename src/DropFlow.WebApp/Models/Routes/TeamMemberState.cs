using DropFlow.Domain.Enums;

namespace DropFlow.WebApp.Models.Routes;

/// <summary>
/// Membre d'équipe dans le wizard
/// </summary>
public class TeamMemberState
{
    public int DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public TeamMemberRole Role { get; set; }
}