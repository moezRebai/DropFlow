using DropFlow.Domain.Common;

namespace DropFlow.Domain.Entities;

public class TimeSlot : ITenantEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;  // "Matin", "Après-midi", etc.
    public TimeSpan StartTime { get; set; }            // 08:00
    public TimeSpan EndTime { get; set; }              // 12:00
    
    public int DisplayOrder { get; set; } = 0;
    
    // Navigation
    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}