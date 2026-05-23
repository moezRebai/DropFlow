using DropFlow.Shared.Common;

namespace DropFlow.Shared.Stores;

public class StoreFilterDto : PaginatedFilter
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}
