namespace DropFlow.Shared.Dashboard;

/// <summary>
/// DTO pour une notification
/// </summary>
public class NotificationDto
{
    public long Id { get; set; }
    public string Type { get; set; } = "Info"; // Success, Warning, Error, Info
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = string.Empty;
    
    /// <summary>
    /// Temps écoulé depuis la notification
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