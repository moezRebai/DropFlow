using DropFlow.Shared.Common;

namespace DropFlow.Shared.Drivers;

public class DriverFilterDto : PaginatedFilter
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}
