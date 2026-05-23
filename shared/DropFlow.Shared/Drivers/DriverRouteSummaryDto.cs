namespace DropFlow.Shared.Drivers;

public class DriverRouteSummaryDto
{
    public int RouteId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public int TotalDeliveries { get; set; }
    public int DeliveredCount { get; set; }
    public TimeSpan? StartTime { get; set; }
    public decimal TotalDistanceKm { get; set; }
}
