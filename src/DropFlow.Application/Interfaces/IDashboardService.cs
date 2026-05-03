using DropFlow.Shared.Dashboard;

namespace DropFlow.Application.Interfaces;

/// <summary>
/// Interface pour le service Dashboard (Application Layer)
/// Fournit les statistiques et données pour le tableau de bord Manager
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Récupère les statistiques KPI du dashboard
    /// - Livraisons non planifiées
    /// - Livraisons du jour (total et livrées)
    /// - Revenus mensuels avec tendance
    /// - Tournées actives
    /// </summary>
    Task<DashboardStatsDto> GetStatsAsync();

    /// <summary>
    /// Récupère les livraisons prévues aujourd'hui
    /// Inclut : Référence, Client, Adresse, Heure, Statut, Chauffeur
    /// </summary>
    Task<List<TodayDeliveryDto>> GetTodayDeliveriesAsync();

    /// <summary>
    /// Récupère les livraisons à risque nécessitant une attention
    /// - Retard probable
    /// - 2e tentative de livraison
    /// - Client VIP
    /// - Autres alertes
    /// </summary>
    Task<List<RiskyDeliveryDto>> GetRiskyDeliveriesAsync();

    /// <summary>
    /// Récupère les notifications récentes depuis AuditLogs
    /// Types : Success, Warning, Error, Info
    /// </summary>
    /// <param name="count">Nombre de notifications (max 50)</param>
    Task<List<NotificationDto>> GetNotificationsAsync(int count = 10);

    /// <summary>
    /// Récupère les événements récents pour la timeline
    /// Exemples : Tournée démarrée, Livraison réussie, Nouvelle commande
    /// </summary>
    /// <param name="count">Nombre d'événements (max 50)</param>
    Task<List<EventDto>> GetRecentEventsAsync(int count = 10);

    /// <summary>
    /// Récupère les données du graphique Revenus + Livraisons
    /// Adapte les labels et données selon la période (Semaine/Mois/Année)
    /// </summary>
    Task<RevenueChartDataDto> GetRevenueChartDataAsync(ChartPeriod period);

    /// <summary>
    /// Récupère les données du graphique Status (Donut)
    /// Distribution : Livrées, Non planifiées, Planifiées, Annulées
    /// </summary>
    Task<StatusChartDataDto> GetStatusChartDataAsync(ChartPeriod period);

    /// <summary>
    /// Récupère les données du graphique Livraisons par magasin
    /// Top 5 magasins par revenus + catégorie "Autres"
    /// </summary>
    Task<StoreChartDataDto> GetStoreChartDataAsync(ChartPeriod period);
}
