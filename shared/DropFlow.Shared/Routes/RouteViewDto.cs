using DropFlow.Shared.Enums;

namespace DropFlow.Shared.Routes;

public class RouteViewDto
{
    public int Id { get; set; }
    public string Reference { get; set; }
    public DateTime Date { get; set; }
    public string VehicleName { get; set; }
    public RouteStatus Status { get; set; }
    public string StatusDisplay { get; set; }
    public int TotalDeliveries { get; set; }
    public decimal TotalDistance { get; set; }
    public int TotalDuration { get; set; }
    public string MainDriverName { get; set; }
    public int TeamCount { get; set; }
}