using DropFlow.Shared.Enums;

namespace DropFlow.Shared.Drivers;

/// <summary>
/// Livraison en liste dans la feuille de route du livreur
/// Informations minimales pour l'écran principal
/// </summary>
public class DriverDeliveryListDto
{
    public int Id { get; set; }
    public int SequenceOrder { get; set; }
    public string Reference { get; set; } = string.Empty;
    
    // Client (résumé)
    public string ClientName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    
    // Créneau
    public string? TimeSlotName { get; set; }
    public TimeSpan? EstimatedArrivalTime { get; set; }
    
    // Statut
    public DeliveryStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    
    // Indicateurs visuels
    public bool WithAssembly { get; set; }
    public int TotalPackages { get; set; }
    public bool HasClientPayment { get; set; }
    public bool IsClientAbsent { get; set; }
    public bool IsValidated { get; set; }
}
