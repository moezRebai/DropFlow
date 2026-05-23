namespace DropFlow.WebApp.Models.Routes;

/// <summary>
/// Livraison optimisée avec créneaux horaires et données de trajet
/// </summary>
public class OptimizedDeliveryState
{
    public int DeliveryId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // ═══ ORDRE & TIMING ═══
    public int SequenceOrder { get; set; }
    public TimeSpan EstimatedArrivalTime { get; set; }
    
    // ═══ TRAJET (depuis Google Maps) ═══
    public int DurationMinutes { get; set; }           // ⚠️ ATTENTION: C'est TravelDurationMinutes dans le backend
    public int DistanceToNextMeters { get; set; }
    
    // ═══ DÉPART ═══
    public string DepartureAddress { get; set; } = string.Empty;
    public TimeSpan DepartureTime { get; set; }
    
    // ═══ PRESTATION ═══
    public int ServiceDurationMinutes { get; set; } = 15;
    
    // ═══ CRÉNEAUX HORAIRES ═══
    public TimeSpan TimeSlotStart { get; set; }
    public TimeSpan TimeSlotEnd { get; set; }
}