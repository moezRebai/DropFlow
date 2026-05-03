namespace DropFlow.Shared.Dashboard;

/// <summary>
/// DTO pour un événement de la timeline
/// </summary>
public class EventDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "Info"; // Success, Error, Info
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Temps écoulé depuis l'événement
    /// </summary>
    public string TimeAgo
    {
        get
        {
            var diff = DateTime.UtcNow - Timestamp;
            if (diff.TotalMinutes < 1) return "À l'instant";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h";
            return $"{(int)diff.TotalDays}j";
        }
    }
}