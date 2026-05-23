namespace DropFlow.Shared.Vehicles;

public class VehicleDto
{
    public int Id { get; set; }
    public string Brand { get; set; }
    public string Model { get; set; }
    public string PlateNumber { get; set; }
    public int MaxDeliveries { get; set; }
    public int MaxVolume { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    
    public string DisplayName => $"{Brand} {Model} ({PlateNumber})";
}