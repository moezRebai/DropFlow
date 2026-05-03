namespace DropFlow.Shared.Vehicles;

public class UpdateVehicleDto
{
    public string Brand { get; set; }
    public string Model { get; set; }
    public string PlateNumber { get; set; }
    public int MaxDeliveries { get; set; }
    public int MaxVolume { get; set; }
    public bool IsActive { get; set; }
}