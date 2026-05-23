namespace DropFlow.Shared.Drivers;

/// <summary>
/// Réponse de GET /api/driver/route/today
/// Gère le cas où le livreur n'a pas de tournée
/// </summary>
public class DriverTodayResponse
{
    public bool HasRoute { get; set; }
    public string? Message { get; set; }
    public DriverRouteDto? Route { get; set; }
}
