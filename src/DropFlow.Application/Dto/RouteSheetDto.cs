namespace DropFlow.Application.Dto;

/// <summary>
/// DTO pour la génération de la feuille de route PDF
/// </summary>
public class RouteSheetDto
{
    // En-tête Entreprise
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string CompanyCity { get; set; } = string.Empty;
    public string CompanyPhone { get; set; } = string.Empty;
    public string CompanySiret { get; set; } = string.Empty;
    public string? CompanyLogoUrl { get; set; }
    
    /// <summary>
    /// Acronyme de l'entreprise pour les colonnes (ex: "SRS")
    /// </summary>
    public string CompanyAcronym { get; set; } = "SRS";

    // Informations Route
    public string RouteReference { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    public DateTime RouteDate { get; set; }
    public string TeamMembers { get; set; } = string.Empty;

    // Point de départ
    public string DepartureAddress { get; set; } = string.Empty;
    public TimeSpan DepartureTime { get; set; }

    // Livraisons
    public List<RouteSheetDeliveryDto> Deliveries { get; set; } = new();

    // Totaux
    public decimal TotalStorePayment { get; set; }      // Total CRT MAG
    public decimal TotalClientPayment { get; set; }     // Total CRT {CompanyAcronym}

    // QR Code
    public string? QrCodeData { get; set; }

    // Notes
    public string? Notes { get; set; }
}