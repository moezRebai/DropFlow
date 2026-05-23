namespace DropFlow.Shared.Drivers;

public class DriverAvailabilityDto
{
    public int DriverId { get; set; }
    public string DriverName { get; set; }
    public bool IsAvailable { get; set; }
    public string? ConflictReason { get; set; }
    public ConflictType? ConflictType { get; set; }
}