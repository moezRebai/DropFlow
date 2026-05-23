using DropFlow.Shared.Enums;

namespace DropFlow.Shared.Deliveries;

public class DeliveryViewDto
{
    public int Id { get; set; }
    public int SequentialNumber { get; set; }
    public string Reference { get; set; }
    public DeliveryType Type { get; set; }
    public string ClientName { get; set; }
    public string City { get; set; }
    public string FullAddress { get; set; }
    public string StoreName { get; set; }
    
    public string? InternalNotes { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public decimal Price { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DeliveryStatus Status { get; set; }
    public int? RouteId { get; set; }
    public string? RouteReference { get; set; }
    public string? UrgentDriverName { get; set; }
    public bool WithAssembly { get; set; }
    public int TotalPackages { get; set; }
}