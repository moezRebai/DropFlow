namespace DropFlow.Shared.Routes;

public class OptimizePathResponseDto
{
    public List<OptimizedDeliveryDto> Deliveries { get; set; }
    public decimal TotalDistanceKm { get; set; }
    public int TotalDurationMinutes { get; set; }
}