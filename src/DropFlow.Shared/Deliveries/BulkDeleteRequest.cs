namespace DropFlow.Shared.Deliveries;

public class BulkDeleteRequest
{
    public List<int> DeliveryIds { get; set; } = new();
}