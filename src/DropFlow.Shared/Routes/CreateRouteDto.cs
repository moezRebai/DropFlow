namespace DropFlow.Shared.Routes;

public class CreateRouteDto
{
    public DateTime Date { get; set; }
    public int VehicleId { get; set; }
    public TimeSpan StartTime { get; set; }
    public string DepartureAddress { get; set; } = string.Empty; // Obligatoire
    public double? DepartureLatitude { get; set; }
    public double? DepartureLongitude { get; set; }
    public List<TeamMemberDto> Team { get; set; } = [];
    public List<CreateDeliverySequenceDto> Deliveries { get; set; } = [];
    public decimal TotalDistance { get; set; }
    public int TotalDuration { get; set; }
    public bool WasOptimizedByGoogle { get; set; }
    public bool WasManuallyReordered { get; set; }
}