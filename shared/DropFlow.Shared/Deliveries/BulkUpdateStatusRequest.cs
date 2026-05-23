using DropFlow.Shared.Enums;

namespace DropFlow.Shared.Deliveries;

public class BulkUpdateStatusRequest
{
    public List<int> DeliveryIds { get; set; } = new();
    public DeliveryStatus Status { get; set; }
}