namespace DropFlow.Shared.Dashboard;

/// <summary>
/// DTO pour une livraison dans la table du jour
/// </summary>
public class TodayDeliveryDto
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string DeliveryCity { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public TimeSpan? ScheduledTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public bool IsLate { get; set; }
}