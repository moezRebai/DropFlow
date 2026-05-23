using DropFlow.Shared.Common;

namespace DropFlow.Shared.Tenants.Depots;

public class TenantDepotFilterDto : PaginatedFilter
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDefault { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
}
