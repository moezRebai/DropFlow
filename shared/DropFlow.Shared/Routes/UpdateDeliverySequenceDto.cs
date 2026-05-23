namespace DropFlow.Shared.Routes;

/// <summary>
/// DTO pour mettre à jour la séquence d'une livraison dans une tournée
/// </summary>
public class UpdateDeliverySequenceDto
{
    public int DeliveryId { get; set; }
    public int SequenceOrder { get; set; }
    
    // ✅ Optimisation - Départ
    public string? DepartureAddress { get; set; }
    public TimeSpan? DepartureTime { get; set; }
    
    // ✅ Optimisation - Arrivée
    public TimeSpan? EstimatedArrivalTime { get; set; }
    
    // ✅ Optimisation - Trajet
    public int? TravelDurationMinutes { get; set; }
    public int? DistanceToNextMeters { get; set; }
    
    // ✅ TimeSlot (optionnel)
    public TimeSpan? TimeSlotStart { get; set; }
    public TimeSpan? TimeSlotEnd { get; set; }
}