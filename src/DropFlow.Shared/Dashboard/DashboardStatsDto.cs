namespace DropFlow.Shared.Dashboard;

/// <summary>
/// DTO pour les statistiques KPI du dashboard
/// </summary>
public class DashboardStatsDto
{
    /// <summary>
    /// Nombre de livraisons non planifiées (ToBePlanned)
    /// </summary>
    public int UnplannedDeliveries { get; set; }
    
    /// <summary>
    /// Tendance vs hier
    /// </summary>
    public int UnplannedTrend { get; set; }
    
    /// <summary>
    /// Nombre total de livraisons aujourd'hui
    /// </summary>
    public int TodayDeliveries { get; set; }
    
    /// <summary>
    /// Nombre de livraisons déjà livrées aujourd'hui
    /// </summary>
    public int DeliveredToday { get; set; }
    
    /// <summary>
    /// Chiffre d'affaires du mois en cours (€)
    /// </summary>
    public decimal MonthlyRevenue { get; set; }
    
    /// <summary>
    /// Tendance CA vs mois dernier (%)
    /// </summary>
    public decimal RevenueTrend { get; set; }
    
    /// <summary>
    /// Nombre de tournées actives aujourd'hui
    /// </summary>
    public int ActiveRoutes { get; set; }
    
    /// <summary>
    /// Nombre total de tournées planifiées aujourd'hui
    /// </summary>
    public int TotalRoutesToday { get; set; }
}