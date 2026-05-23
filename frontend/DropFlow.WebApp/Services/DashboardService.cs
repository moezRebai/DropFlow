using DropFlow.Shared.Dashboard;
using DropFlow.WebApp.Interfaces;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services;

/// <summary>
/// Service Dashboard Frontend - Appelle l'API Dashboard pour récupérer les données
/// </summary>
public class DashboardService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger<DashboardService> logger)
    : BaseApiService(httpClientFactory, localStorage, logger), IDashboardService
{
    /// <summary>
    /// Récupère les statistiques KPI
    /// GET /api/dashboard/stats
    /// </summary>
    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        try
        {
            Logger.LogInformation("[Dashboard] 📊 Loading stats...");
            
            var stats = await GetAsync<DashboardStatsDto>("api/dashboard/stats");
            
            if (stats != null)
            {
                Logger.LogInformation("[Dashboard] ✅ Stats loaded successfully");
                return stats;
            }

            Logger.LogWarning("[Dashboard] ⚠️ No stats returned from API");
            return new DashboardStatsDto();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Dashboard] ❌ Error loading stats");
            return new DashboardStatsDto();
        }
    }

    /// <summary>
    /// Récupère les livraisons du jour
    /// GET /api/dashboard/today-deliveries
    /// </summary>
    public async Task<List<TodayDeliveryDto>> GetTodayDeliveriesAsync()
    {
        try
        {
            Logger.LogInformation("[Dashboard] 📦 Loading today's deliveries...");
            
            var deliveries = await GetAsync<List<TodayDeliveryDto>>("api/dashboard/today-deliveries");
            
            if (deliveries != null)
            {
                Logger.LogInformation("[Dashboard] ✅ Loaded {Count} deliveries", deliveries.Count);
                return deliveries;
            }

            Logger.LogWarning("[Dashboard] ⚠️ No deliveries returned from API");
            return new List<TodayDeliveryDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Dashboard] ❌ Error loading today's deliveries");
            return new List<TodayDeliveryDto>();
        }
    }

    /// <summary>
    /// Récupère les livraisons à risque
    /// GET /api/dashboard/risky-deliveries
    /// </summary>
    public async Task<List<RiskyDeliveryDto>> GetRiskyDeliveriesAsync()
    {
        try
        {
            Logger.LogInformation("[Dashboard] ⚠️ Loading risky deliveries...");
            
            var riskyDeliveries = await GetAsync<List<RiskyDeliveryDto>>("api/dashboard/risky-deliveries");
            
            if (riskyDeliveries != null)
            {
                Logger.LogInformation("[Dashboard] ✅ Loaded {Count} risky deliveries", riskyDeliveries.Count);
                return riskyDeliveries;
            }

            Logger.LogWarning("[Dashboard] ⚠️ No risky deliveries returned from API");
            return new List<RiskyDeliveryDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Dashboard] ❌ Error loading risky deliveries");
            return new List<RiskyDeliveryDto>();
        }
    }

    /// <summary>
    /// Récupère les notifications récentes
    /// GET /api/dashboard/notifications?count={count}
    /// </summary>
    public async Task<List<NotificationDto>> GetNotificationsAsync(int count = 10)
    {
        try
        {
            Logger.LogInformation("[Dashboard] 🔔 Loading {Count} notifications...", count);
            
            var notifications = await GetAsync<List<NotificationDto>>($"api/dashboard/notifications?count={count}");
            
            if (notifications != null)
            {
                Logger.LogInformation("[Dashboard] ✅ Loaded {Count} notifications", notifications.Count);
                return notifications;
            }

            Logger.LogWarning("[Dashboard] ⚠️ No notifications returned from API");
            return new List<NotificationDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Dashboard] ❌ Error loading notifications");
            return new List<NotificationDto>();
        }
    }

    /// <summary>
    /// Récupère les événements récents
    /// GET /api/dashboard/events?count={count}
    /// </summary>
    public async Task<List<EventDto>> GetRecentEventsAsync(int count = 10)
    {
        try
        {
            Logger.LogInformation("[Dashboard] 📅 Loading {Count} events...", count);
            
            var events = await GetAsync<List<EventDto>>($"api/dashboard/events?count={count}");
            
            if (events != null)
            {
                Logger.LogInformation("[Dashboard] ✅ Loaded {Count} events", events.Count);
                return events;
            }

            Logger.LogWarning("[Dashboard] ⚠️ No events returned from API");
            return new List<EventDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Dashboard] ❌ Error loading events");
            return new List<EventDto>();
        }
    }

    /// <summary>
    /// Récupère les données du graphique Revenus + Livraisons
    /// GET /api/dashboard/revenue-chart?period={period}
    /// </summary>
    public async Task<RevenueChartDataDto> GetRevenueChartDataAsync(ChartPeriod period)
    {
        try
        {
            Logger.LogInformation("[Dashboard] 📈 Loading revenue chart for {Period}...", period);
            
            var chartData = await GetAsync<RevenueChartDataDto>($"api/dashboard/revenue-chart?period={period}");
            
            if (chartData != null)
            {
                Logger.LogInformation("[Dashboard] ✅ Revenue chart loaded ({Points} points)", chartData.Labels.Count);
                return chartData;
            }

            Logger.LogWarning("[Dashboard] ⚠️ No revenue chart data returned from API");
            return new RevenueChartDataDto();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Dashboard] ❌ Error loading revenue chart");
            return new RevenueChartDataDto();
        }
    }

    /// <summary>
    /// Récupère les données du graphique Status
    /// GET /api/dashboard/status-chart?period={period}
    /// </summary>
    public async Task<StatusChartDataDto> GetStatusChartDataAsync(ChartPeriod period)
    {
        try
        {
            Logger.LogInformation("[Dashboard] 🍩 Loading status chart for {Period}...", period);
            
            var chartData = await GetAsync<StatusChartDataDto>($"api/dashboard/status-chart?period={period}");
            
            if (chartData != null)
            {
                Logger.LogInformation("[Dashboard] ✅ Status chart loaded ({Count} statuses)", chartData.Labels.Count);
                return chartData;
            }

            Logger.LogWarning("[Dashboard] ⚠️ No status chart data returned from API");
            return new StatusChartDataDto();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Dashboard] ❌ Error loading status chart");
            return new StatusChartDataDto();
        }
    }

    /// <summary>
    /// Récupère les données du graphique Magasins
    /// GET /api/dashboard/store-chart?period={period}
    /// </summary>
    public async Task<StoreChartDataDto> GetStoreChartDataAsync(ChartPeriod period)
    {
        try
        {
            Logger.LogInformation("[Dashboard] 🏪 Loading store chart for {Period}...", period);
            
            var chartData = await GetAsync<StoreChartDataDto>($"api/dashboard/store-chart?period={period}");
            
            if (chartData != null)
            {
                Logger.LogInformation("[Dashboard] ✅ Store chart loaded ({Count} stores)", chartData.StoreNames.Count);
                return chartData;
            }

            Logger.LogWarning("[Dashboard] ⚠️ No store chart data returned from API");
            return new StoreChartDataDto();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Dashboard] ❌ Error loading store chart");
            return new StoreChartDataDto();
        }
    }
}
