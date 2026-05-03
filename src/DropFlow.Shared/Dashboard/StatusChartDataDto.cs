namespace DropFlow.Shared.Dashboard;

/// <summary>
/// DTO pour les données du graphique Status
/// </summary>
public class StatusChartDataDto
{
    /// <summary>
    /// Labels des status
    /// </summary>
    public List<string> Labels { get; set; } = new();
    
    /// <summary>
    /// Valeurs pour chaque status
    /// </summary>
    public List<double> Values { get; set; } = new();
}