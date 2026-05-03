using DropFlow.Domain.Enums;

namespace DropFlow.Shared.Deliveries;

public class CreateDeliveryDto : DeliveryBaseDto
{
    public int StoreId { get; set; }
    public string FileNumber { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public decimal Price { get; set; }
    public decimal? ClientPaymentAmount { get; set; }
    public decimal? StorePaymentAmount { get; set; }

    public DeliveryStatus Status { get; set; }
    public DeliveryType Type { get; set; } = DeliveryType.Standard;
    public int? UrgentDriverId { get; set; }
    
    public bool WithAssembly { get; set; }
    public string? DeliveryNotes { get; set; }
    public string? InternalNotes { get; set; }

    public int? EstimatedDurationMinutes { get; set; }
    public int? TimeSlotId { get; set; }
    public List<CreateDeliveryItemDto> Items { get; set; }
}