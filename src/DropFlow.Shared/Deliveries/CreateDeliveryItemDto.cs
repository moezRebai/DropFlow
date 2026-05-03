namespace DropFlow.Shared.Deliveries;

public class CreateDeliveryItemDto
{
    public string? Reference { get; set; }
    public string Designation { get; set; }
    public int Quantity { get; set; }
    public string? Information { get; set; }
}