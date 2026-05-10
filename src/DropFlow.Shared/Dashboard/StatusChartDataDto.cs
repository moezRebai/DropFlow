namespace DropFlow.Shared.Dashboard;

/// <summary>
/// DTO pour les données du graphique Status
/// </summary>
public class StatusChartDataDto
{
    public List<string> Labels { get; set; } = new();
    public List<double> Values { get; set; } = new();

    /// <summary>
    /// Nombre de livraisons avec statut Delivered sur la période — calculé via DeliveryStatus.Delivered
    /// </summary>
    public int DeliveredCount { get; set; }

    /// <summary>
    /// Total de livraisons sur la période (tous statuts confondus)
    /// </summary>
    public int TotalCount { get; set; }
}