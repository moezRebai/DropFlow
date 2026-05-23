namespace DropFlow.Application.Dto;

/// <summary>
/// DTO pour une ligne de livraison dans la feuille de route
/// </summary>
public class RouteSheetDeliveryDto
{
    public int SequenceOrder { get; set; }
    
    /// <summary>
    /// N° Dossier (FileNumber)
    /// </summary>
    public string DeliveryReference { get; set; } = string.Empty;
    
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Heure estimée d'arrivée (colonne H)
    /// </summary>
    public TimeSpan? EstimatedArrivalTime { get; set; }
    
    /// <summary>
    /// Type de prestation (M = Montage, N = Normal)
    /// Basé sur Delivery.WithAssembly
    /// </summary>
    public string ServiceType { get; set; } = "N";
    
    /// <summary>
    /// Enseigne/Magasin d'origine (Store.Name)
    /// </summary>
    public string? StoreName { get; set; }
    
    /// <summary>
    /// Montant à payer pour l'enseigne (CRT MAG)
    /// Correspond à Delivery.StorePaymentAmount
    /// </summary>
    public decimal StorePaymentAmount { get; set; }
    
    /// <summary>
    /// Montant à payer pour l'entreprise de livraison (CRT SRS/CompanyAcronym)
    /// Correspond à Delivery.ClientPaymentAmount
    /// </summary>
    public decimal ClientPaymentAmount { get; set; }
    
    /// <summary>
    /// Instructions spécifiques (Delivery.DeliveryNotes)
    /// </summary>
    public string? Instructions { get; set; }
    
    /// <summary>
    /// Créneaux horaires (si TimeSlot existe)
    /// </summary>
    public TimeSpan? TimeSlotStart { get; set; }
    public TimeSpan? TimeSlotEnd { get; set; }
}