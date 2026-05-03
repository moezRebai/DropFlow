namespace DropFlow.Shared.Drivers;

public class CreateDriverDto
{
    public string UserId { get; set; }
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiryDate { get; set; }
    public string? VehicleType { get; set; }
}