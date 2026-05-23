namespace DropFlow.Shared.Routes;

/// <summary>
/// DTO pour la mise à jour complète d'une tournée
/// </summary>
public class UpdateRouteDto
{
    // ✅ Informations de base
    public DateTime Date { get; set; }
    public int VehicleId { get; set; }
    public TimeSpan StartTime { get; set; }
    public string DepartureAddress { get; set; } = string.Empty;
    public double? DepartureLatitude { get; set; }
    public double? DepartureLongitude { get; set; }
    public string? Notes { get; set; }

    // ✅ Équipe
    public List<TeamMemberDto> Team { get; set; } = new();

    // ✅ Livraisons avec séquence
    public List<UpdateDeliverySequenceDto> Deliveries { get; set; } = new();

    // ✅ Métriques
    public decimal TotalDistance { get; set; }
    public int TotalDuration { get; set; }
    public bool WasOptimizedByGoogle { get; set; }
    public bool WasManuallyReordered { get; set; }
}