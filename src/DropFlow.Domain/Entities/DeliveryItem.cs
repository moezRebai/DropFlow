namespace DropFlow.Domain.Entities;

public class DeliveryItem
{
    public int Id { get; set; }
    public int DeliveryId { get; set; }
    public string? Reference { get; set; }
    public string Designation { get; set; }
    public int Quantity { get; set; }
    public string? Information { get; set; }
    
    // Navigation
    public Delivery Delivery { get; set; }
}