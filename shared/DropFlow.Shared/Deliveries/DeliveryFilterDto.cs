using DropFlow.Shared.Common;
using DropFlow.Shared.Enums;

namespace DropFlow.Shared.Deliveries;

public class DeliveryFilterDto : PaginatedFilter
{
    public int? StoreId { get; set; }
    public DeliveryType? Type { get; set; }
    public List<DeliveryStatus>? Statuses { get; set; }
    public string? ClientSearch { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? RouteId { get; set; }
    public int? UrgentDriverId { get; set; }
    public bool? WithAssembly { get; set; }
    public string? GlobalSearch { get; set; }
    public string SortBy { get; set; } = "SequentialNumber";
    public bool SortDescending { get; set; } = true;
}
