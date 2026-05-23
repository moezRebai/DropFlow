using DropFlow.Shared.Enums;

namespace DropFlow.Shared.Drivers;

/// <summary>
/// Tournée du jour vue par le livreur
/// Retournée par GET /api/driver/route/today
/// </summary>
public class DriverRouteDto
{
    public int RouteId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public string DepartureAddress { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan? EstimatedEndTime { get; set; }
    public RouteStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    
    // Métriques
    public int TotalDeliveries { get; set; }
    public decimal TotalDistanceKm { get; set; }
    public int TotalDurationMinutes { get; set; }
    
    // Équipe
    public List<string> TeamMembers { get; set; } = new();
    
    // Livraisons ordonnées par SequenceOrder
    public List<DriverDeliveryListDto> Deliveries { get; set; } = new();
}
