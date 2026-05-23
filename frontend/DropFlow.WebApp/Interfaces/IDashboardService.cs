using DropFlow.Shared.Dashboard;

namespace DropFlow.WebApp.Interfaces;

/// <summary>
/// Interface pour le service Dashboard
/// Charge toutes les données affichées sur le dashboard Manager
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Récupère les statistiques KPI du dashboard
    /// </summary>
    Task<DashboardStatsDto> GetStatsAsync();
    
    /// <summary>
    /// Récupère les livraisons du jour
    /// </summary>
    Task<List<TodayDeliveryDto>> GetTodayDeliveriesAsync();
    
    /// <summary>
    /// Récupère les livraisons à risque
    /// </summary>
    Task<List<RiskyDeliveryDto>> GetRiskyDeliveriesAsync();
    
    /// <summary>
    /// Récupère les notifications récentes
    /// </summary>
    Task<List<NotificationDto>> GetNotificationsAsync(int count = 10);
    
    /// <summary>
    /// Récupère les événements récents (timeline)
    /// </summary>
    Task<List<EventDto>> GetRecentEventsAsync(int count = 10);
    
    /// <summary>
    /// Récupère les données du graphique Revenus + Livraisons
    /// </summary>
    Task<RevenueChartDataDto> GetRevenueChartDataAsync(ChartPeriod period);
    
    /// <summary>
    /// Récupère les données du graphique Status des livraisons
    /// </summary>
    Task<StatusChartDataDto> GetStatusChartDataAsync(ChartPeriod period);
    
    /// <summary>
    /// Récupère les données du graphique Livraisons par magasin
    /// </summary>
    Task<StoreChartDataDto> GetStoreChartDataAsync(ChartPeriod period);
}
