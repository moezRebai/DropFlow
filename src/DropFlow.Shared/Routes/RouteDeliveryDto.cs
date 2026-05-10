namespace DropFlow.Shared.Routes;

public class RouteDeliveryDto
{
    public int Id { get; set; }
    public int DeliveryId { get; set; }
    public string Reference { get; set; }
    public string ClientName { get; set; }
    public string Address { get; set; }
    
    // ✅ Coordonnées GPS
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // ✅ Optimisation - Ordre
    public int? SequenceOrder { get; set; }
    
    // ✅ Optimisation - Départ
    public string? DepartureAddress { get; set; }
    public TimeSpan? DepartureTime { get; set; }
    
    // ✅ Optimisation - Arrivée
    public TimeSpan? EstimatedArrivalTime { get; set; }
    
    // ✅ Optimisation - Trajet
    public int? TravelDurationMinutes { get; set; }
    public int? DistanceToNextMeters { get; set; }
    
    // ✅ Prestation
    public int EstimatedDurationMinutes { get; set; }
    
    // ✅ TimeSlot (optionnel)
    public int? TimeSlotId { get; set; }
    public string? TimeSlotName { get; set; }

    public int ItemCount { get; set; }
}