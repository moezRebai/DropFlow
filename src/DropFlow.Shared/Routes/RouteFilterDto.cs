using DropFlow.Domain.Enums;

namespace DropFlow.Shared.Routes;

public class RouteFilterDto
{
    public DateTime? Date { get; set; }
    public RouteStatus? Status { get; set; }
    public int? VehicleId { get; set; }
    public int? DriverId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}