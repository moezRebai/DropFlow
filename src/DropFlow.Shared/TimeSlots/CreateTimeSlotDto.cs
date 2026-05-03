namespace DropFlow.Shared.TimeSlots;

public class CreateTimeSlotDto
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}