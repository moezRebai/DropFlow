namespace DropFlow.Shared.Dashboard;

/// <summary>
/// DTO pour les données du graphique Magasins
/// </summary>
public class StoreChartDataDto
{
    /// <summary>
    /// Noms des magasins
    /// </summary>
    public List<string> StoreNames { get; set; } = new();
    
    /// <summary>
    /// Revenus par magasin (en k€)
    /// </summary>
    public List<double> Revenues { get; set; } = new();
}