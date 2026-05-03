namespace DropFlow.Shared.Routes;

public class OptimizedDeliveryDto
{
    public int DeliveryId { get; set; }
    public int SequenceOrder { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int DistanceToNextMeters { get; set; }
    public int DurationToNextMinutes { get; set; }
}