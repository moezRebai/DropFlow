using DropFlow.Shared.Common;
using DropFlow.Shared.Enums;

namespace DropFlow.Shared.Routes;

public class RouteFilterDto : PaginatedFilter
{
    public string? SearchTerm { get; set; }
    public DateTime? Date { get; set; }
    public RouteStatus? Status { get; set; }
    public int? VehicleId { get; set; }
    public int? DriverId { get; set; }
}
