using DropFlow.Shared.Common;

namespace DropFlow.Shared.Vehicles;

public class VehicleFilterDto : PaginatedFilter
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}
