namespace DropFlow.Shared.Dashboard;

/// <summary>
/// DTO pour une livraison à risque
/// </summary>
public class RiskyDeliveryDto
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string DeliveryCity { get; set; } = string.Empty;
    public DateTime EstimatedTime { get; set; }
    public string RiskReason { get; set; } = string.Empty; // "Retard probable", "2e tentative", etc.
    public string RiskLevel { get; set; } = "Warning"; // Warning, Error, Info
}