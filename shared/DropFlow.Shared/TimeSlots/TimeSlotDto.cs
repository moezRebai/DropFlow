namespace DropFlow.Shared.TimeSlots;

public class TimeSlotDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DisplayOrder { get; set; }
    
    // Computed
    public string DisplayText => $"({StartTime:hh\\:mm} - {EndTime:hh\\:mm})";
   
    public string DurationText
    {
        get
        {
            var duration = EndTime - StartTime;
            return duration.Hours > 0 
                ? $"{duration.Hours}h{(duration.Minutes > 0 ? duration.Minutes.ToString() : "")}" 
                : $"{duration.Minutes}min";
        }
    }
}