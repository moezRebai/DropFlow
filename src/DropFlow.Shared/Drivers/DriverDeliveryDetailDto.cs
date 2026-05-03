using DropFlow.Domain.Enums;

namespace DropFlow.Shared.Drivers;

/// <summary>
/// Détail complet d'une livraison pour le livreur
/// Retourné par GET /api/driver/deliveries/{id}
/// 
/// SÉCURITÉ : Exclut volontairement :
/// - InternalNotes (confidentielles manager)
/// - Price (prix prestation)
/// - StorePaymentAmount (commission magasin)
/// </summary>
public class DriverDeliveryDetailDto
{
    public int Id { get; set; }
    public int SequenceOrder { get; set; }
    public string Reference { get; set; } = string.Empty;
    
    // ═══ CLIENT ═══
    public string ClientFirstName { get; set; } = string.Empty;
    public string ClientLastName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    
    // ═══ ADRESSE ═══
    public string Address { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? AddressComplement { get; set; }
    public string FullAddress { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // ═══ ENSEIGNE ═══
    public string StoreName { get; set; } = string.Empty;
    public string? FileNumber { get; set; }
    
    // ═══ CRÉNEAU ═══
    public DateTime? ScheduledDate { get; set; }
    public string? TimeSlotName { get; set; }
    public TimeSpan? EstimatedArrivalTime { get; set; }
    
    // ═══ PRESTATION ═══
    public bool WithAssembly { get; set; }
    public int TotalPackages { get; set; }
    public decimal? ClientPaymentAmount { get; set; }
    
    // ═══ INSTRUCTIONS CHAUFFEUR ═══
    /// <summary>
    /// Notes visibles chauffeur uniquement (DeliveryNotes)
    /// Les InternalNotes ne sont PAS transmises
    /// </summary>
    public string? DeliveryNotes { get; set; }
    
    // ═══ PRODUITS ═══
    public List<DriverDeliveryItemDto> Items { get; set; } = new();
    
    // ═══ STATUT ═══
    public DeliveryStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    
    // ═══ VALIDATION (si déjà validée) ═══
    public bool IsValidated { get; set; }
    public bool IsClientAbsent { get; set; }
    public string? ValidationComment { get; set; }
    public DateTime? DeliveredDateTime { get; set; }
    public bool HasSignature { get; set; }
    public bool HasPhoto { get; set; }
}

/// <summary>
/// Produit dans une livraison (vue livreur)
/// </summary>
public class DriverDeliveryItemDto
{
    public string? Reference { get; set; }
    public string Designation { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Information { get; set; }
}
