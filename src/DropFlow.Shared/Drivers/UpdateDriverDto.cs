namespace DropFlow.Shared.Drivers;

public class UpdateDriverDto
{
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiryDate { get; set; }
    public string? VehicleType { get; set; }
    public bool IsActive { get; set; } // ← Ajouter cette ligne
}