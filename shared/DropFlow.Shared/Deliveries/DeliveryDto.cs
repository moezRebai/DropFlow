using DropFlow.Shared.Enums;
using DropFlow.Shared.TimeSlots;

namespace DropFlow.Shared.Deliveries;

/// <summary>
/// DTO complet pour affichage d'une livraison (liste + détails)
/// </summary>
public class DeliveryDto
{
    public int Id { get; set; }
    public int SequentialNumber { get; set; }
    public string Reference { get; set; }
    
    // Client
    public int ClientId { get; set; }
    public string ClientName { get; set; }
    public string ClientPhone { get; set; }
    public string? ClientEmail { get; set; }
    
    public ClientDetailDto? Client { get; set; }
    
    // Address
    public int ClientAddressId { get; set; }
    public string Address { get; set; }
    public string ZipCode { get; set; }
    public string City { get; set; }
    public string AddressComplement { get; set; }
    public string AddressLabel { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    // Store
    public int StoreId { get; set; }
    public string StoreName { get; set; }
    
    // Details
    public string FileNumber { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public decimal Price { get; set; }
    public decimal? ClientPaymentAmount { get; set; }
    public decimal? StorePaymentAmount { get; set; }
    
    // Organization
    public DeliveryStatus Status { get; set; }
    public string StatusDisplay { get; set; }
    
    public DeliveryType Type { get; set; }
    public string TypeDisplay { get; set; }
    public int? RouteId { get; set; }
    public string? RouteReference { get; set; }
    public TimeSpan? EstimatedArrivalTime { get; set; }
    public TimeSpan? ActualArrivalTime { get; set; }
    public int? UrgentDriverId { get; set; }
    public string? UrgentDriverName { get; set; }
    
    public int? EstimatedDurationMinutes { get; set; }
    public int? TimeSlotId { get; set; }
    public TimeSlotDto? TimeSlot { get; set; }
    
    public bool WithAssembly { get; set; }
    public string DeliveryNotes { get; set; }
    public string InternalNotes { get; set; }
    // Items
    public List<DeliveryItemDto> Items { get; set; }
    public int TotalPackages { get; set; }
    
    // Audit
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    
    public string? Notes
    {
        get
        {
            var hasDeliveryNotes = !string.IsNullOrWhiteSpace(DeliveryNotes);
            var hasInternalNotes = !string.IsNullOrWhiteSpace(InternalNotes);

            return hasDeliveryNotes switch
            {
                true when hasInternalNotes => $"{DeliveryNotes}{Environment.NewLine}{InternalNotes}",
                true => DeliveryNotes,
                _ => hasInternalNotes ? InternalNotes : null
            };
        }
    }

}