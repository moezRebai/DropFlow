namespace DropFlow.Shared.Deliveries;

public class UpdateDeliveryItemDto
{
    public int? Id { get; set; } // null = nouvel item
    public string? Reference { get; set; }
    public string Designation { get; set; }
    public int Quantity { get; set; }
    public string? Information { get; set; }
}