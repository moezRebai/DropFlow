namespace DropFlow.Shared.Routes;

public class CreateDeliverySequenceDto
{
    public int DeliveryId { get; set; }
    public int SequenceOrder { get; set; }
    
    // ✅ Optimisation - Départ
    public string DepartureAddress { get; set; } = string.Empty;
    public TimeSpan DepartureTime { get; set; }
    
    // ✅ Optimisation - Arrivée
    public TimeSpan EstimatedArrivalTime { get; set; }
    
    // ✅ Optimisation - Trajet
    public int TravelDurationMinutes { get; set; }
    public int DistanceToNextMeters { get; set; }
    
    // ✅ TimeSlot (pour fenêtres horaires)
    public TimeSpan TimeSlotStart { get; set; }
    public TimeSpan TimeSlotEnd { get; set; }
}