using DropFlow.Domain.Enums;

namespace DropFlow.Shared.Routes;

public class RouteDto
{
    public int Id { get; set; }
    public string Reference { get; set; }
    public DateTime Date { get; set; }
    public int VehicleId { get; set; }
    public string VehicleName { get; set; }
    public RouteStatus Status { get; set; }
    public string StatusDisplay { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan? EstimatedEndTime { get; set; }
    public decimal TotalDistance { get; set; }
    public int TotalDuration { get; set; }
    public int TotalDeliveries { get; set; }
    public int? TotalVolume { get; set; }
    public string? DepartureAddress { get; set; }
    public bool WasOptimizedByGoogle { get; set; }
    public bool WasManuallyReordered { get; set; }
    public double? DepartureLatitude { get; set; }
    public double? DepartureLongitude { get; set; }
    public List<RouteTeamDto> TeamMembers { get; set; } = new();
    public List<RouteDeliveryDto> Deliveries { get; set; } = new();
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
}