namespace DropFlow.Shared.Dashboard;

/// <summary>
/// DTO pour les données du graphique Revenus + Livraisons
/// </summary>
public class RevenueChartDataDto
{
    /// <summary>
    /// Labels pour l'axe X (dates/périodes)
    /// </summary>
    public List<string> Labels { get; set; } = new();
    
    /// <summary>
    /// Données des revenus (en k€)
    /// </summary>
    public List<double> Revenues { get; set; } = new();
    
    /// <summary>
    /// Données du nombre de livraisons
    /// </summary>
    public List<double> DeliveryCount { get; set; } = new();
}