using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Enums;
using DropFlow.Shared.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Application.Services;

/// <summary>
/// Service Dashboard - Fournit les statistiques et données pour le tableau de bord
/// </summary>
public class DashboardService(
    IApplicationDbContext context,
    ITenantService tenantService,
    ILogger<DashboardService> logger)
    : IDashboardService
{
    #region Stats KPI

    /// <summary>
    /// Récupère les statistiques KPI du dashboard
    /// </summary>
    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);
            var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var endOfThisMonth = startOfMonth.AddMonths(1);

            // Unplanned counts (two small queries, genuinely different filters)
            var unplannedCount = await context.Deliveries
                .Where(d => d.Status == DeliveryStatus.ToBePlanned)
                .CountAsync();

            var unplannedYesterday = await context.Deliveries
                .Where(d => d.Status == DeliveryStatus.ToBePlanned &&
                           d.CreatedDate.Date == yesterday)
                .CountAsync();

            // Today's deliveries — one query, split in memory
            var todayStatuses = await context.Deliveries
                .Where(d => d.ScheduledDate == today)
                .Select(d => d.Status)
                .ToListAsync();
            var todayDeliveriesCount = todayStatuses.Count;
            var deliveredTodayCount = todayStatuses.Count(s => s == DeliveryStatus.Delivered);

            // Revenue — one query covering both months, split in memory
            var revenueData = await context.Deliveries
                .Where(d => d.ScheduledDate >= startOfLastMonth &&
                           d.ScheduledDate < endOfThisMonth &&
                           d.Status == DeliveryStatus.Delivered)
                .Select(d => new { d.ScheduledDate, d.Price })
                .ToListAsync();
            var monthlyRevenue = revenueData
                .Where(d => d.ScheduledDate >= startOfMonth)
                .Sum(d => d.Price);
            var lastMonthRevenue = revenueData
                .Where(d => d.ScheduledDate < startOfMonth)
                .Sum(d => d.Price);

            // Calcul de la tendance des revenus
            var revenueTrend = lastMonthRevenue > 0
                ? ((monthlyRevenue - lastMonthRevenue) / lastMonthRevenue) * 100
                : 0;

            // Routes today — one query, split in memory
            var todayRouteStatuses = await context.Routes
                .Where(r => r.Date == today)
                .Select(r => r.Status)
                .ToListAsync();
            var activeRoutesCount = todayRouteStatuses
                .Count(s => s == RouteStatus.InProgress || s == RouteStatus.Confirmed);
            var totalRoutesToday = todayRouteStatuses.Count;

            // Drivers on road = distinct drivers assigned to an InProgress route today
            var driversOnRoad = await context.RouteTeams
                .Where(rt => rt.Route.Date == today && rt.Route.Status == RouteStatus.InProgress)
                .Select(rt => rt.DriverId)
                .Distinct()
                .CountAsync();

            // Idle vehicles = active vehicles with no InProgress/Confirmed route today
            var busyVehicleIds = await context.Routes
                .Where(r => r.Date == today &&
                            (r.Status == RouteStatus.InProgress || r.Status == RouteStatus.Confirmed))
                .Select(r => r.VehicleId)
                .Distinct()
                .ToListAsync();

            var idleVehicles = await context.Vehicles
                .Where(v => v.IsActive && !busyVehicleIds.Contains(v.Id))
                .CountAsync();

            return new DashboardStatsDto
            {
                UnplannedDeliveries = unplannedCount,
                UnplannedTrend = unplannedCount - unplannedYesterday,
                TodayDeliveries = todayDeliveriesCount,
                DeliveredToday = deliveredTodayCount,
                MonthlyRevenue = monthlyRevenue,
                RevenueTrend = revenueTrend,
                ActiveRoutes = activeRoutesCount,
                TotalRoutesToday = totalRoutesToday,
                DriversOnRoad = driversOnRoad,
                IdleVehicles = idleVehicles
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Dashboard] Error getting stats");
            return new DashboardStatsDto();
        }
    }

    #endregion

    #region Today Deliveries

    /// <summary>
    /// Récupère les livraisons du jour
    /// </summary>
    public async Task<List<TodayDeliveryDto>> GetTodayDeliveriesAsync()
    {
        try
        {
            var today = DateTime.UtcNow.Date;

            var nowTime = DateTime.UtcNow.TimeOfDay;

            var entities = await context.Deliveries
                .Where(d => d.ScheduledDate == today)
                .Include(d => d.Client)
                .Include(d => d.ClientAddress)
                .Include(d => d.TimeSlot)
                .Include(d => d.UrgentDriver)
                    .ThenInclude(ud => ud!.User)
                .OrderBy(d => d.TimeSlot != null ? d.TimeSlot.StartTime : TimeSpan.Zero)
                .Take(10)
                .ToListAsync();

            var deliveries = entities.Select(d => new TodayDeliveryDto
            {
                Id = d.Id,
                Reference = d.Reference,
                ClientName = d.Client.DisplayName,
                DeliveryAddress = d.ClientAddress.Address,
                DeliveryCity = d.ClientAddress.City,
                ScheduledDate = d.ScheduledDate!.Value,
                ScheduledTime = d.TimeSlot?.StartTime,
                Status = d.Status.ToString(),
                DriverName = d.UrgentDriver != null
                    ? $"{d.UrgentDriver.User.FirstName} {d.UrgentDriver.User.LastName}"
                    : null,
                IsLate = d.Status != DeliveryStatus.Delivered
                      && d.Status != DeliveryStatus.Canceled
                      && d.TimeSlot != null
                      && d.TimeSlot.StartTime < nowTime
            }).ToList();

            logger.LogInformation("[Dashboard] ✅ Loaded {Count} today deliveries", deliveries.Count);
            return deliveries;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Dashboard] Error getting today deliveries");
            return new List<TodayDeliveryDto>();
        }
    }

    #endregion

    #region Risky Deliveries

    /// <summary>
    /// Récupère les livraisons à risque
    /// Logique : Livraisons ToBePlanned créées il y a plus de 7 jours
    /// → Le manager doit appeler le client pour planifier
    /// </summary>
    public async Task<List<RiskyDeliveryDto>> GetRiskyDeliveriesAsync()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var threeDaysAgo = today.AddDays(-3);

            // À planifier en urgence = ToBePlanned depuis plus de 3 jours
            var riskyDeliveries = await context.Deliveries
                .Where(d => d.Status == DeliveryStatus.ToBePlanned &&
                           d.CreatedDate.Date <= threeDaysAgo)
                .Include(d => d.Client)
                .Include(d => d.ClientAddress)
                .Include(d => d.TimeSlot)
                .OrderBy(d => d.CreatedDate)
                .Take(10)
                .Select(d => new RiskyDeliveryDto
                {
                    Id = d.Id,
                    Reference = d.Reference,
                    ClientName = d.Client.DisplayName,
                    DeliveryAddress = d.ClientAddress.Address,
                    DeliveryCity = d.ClientAddress.City,
                    EstimatedTime = d.ScheduledDate.HasValue
                        ? d.ScheduledDate.Value.Add(d.TimeSlot != null ? d.TimeSlot.StartTime : TimeSpan.Zero)
                        : DateTime.Now,
                    RiskReason = CalculateRiskReason(d.CreatedDate),
                    RiskLevel = CalculateRiskLevel(d.CreatedDate)
                })
                .ToListAsync();

            logger.LogInformation("[Dashboard] ✅ Loaded {Count} deliveries to plan urgently (ToBePlanned > 3 days)", riskyDeliveries.Count);
            return riskyDeliveries;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Dashboard] Error getting risky deliveries");
            return new List<RiskyDeliveryDto>();
        }
    }

    /// <summary>
    /// Calcule la raison du risque en fonction de l'ancienneté
    /// </summary>
    private string CalculateRiskReason(DateTime createdDate)
    {
        var daysOld = (DateTime.UtcNow.Date - createdDate.Date).Days;
        
        return daysOld switch
        {
            >= 14 => $"Non planifiée depuis {daysOld} jours !",
            >= 7  => $"Non planifiée depuis {daysOld} jours",
            >= 3  => $"À planifier ({daysOld} jours)",
            _     => "À planifier"
        };
    }

    /// <summary>
    /// Calcule le niveau de risque en fonction de l'ancienneté
    /// </summary>
    private string CalculateRiskLevel(DateTime createdDate)
    {
        var daysOld = (DateTime.UtcNow.Date - createdDate.Date).Days;
        
        return daysOld switch
        {
            >= 7 => "Error",    // Très urgent (14+ jours)
            >= 5  => "Warning",  // Urgent (7-13 jours)
            >= 3  => "Info",     // À surveiller (3-6 jours)
            _     => "Info"
        };
    }

    #endregion

    #region Notifications & Events

    /// <summary>
    /// Récupère les notifications récentes depuis AuditLogs
    /// </summary>
    public async Task<List<NotificationDto>> GetNotificationsAsync(int count = 10)
    {
        try
        {
            var tenantId = tenantService.GetTenantId();
            var recentLogs = await context.AuditLogs
                .Where(a => a.TenantId == tenantId)
                .OrderByDescending(a => a.Timestamp)
                .Take(Math.Min(count, 50))
                .ToListAsync();

            var notifications = recentLogs.Select(log => new NotificationDto
            {
                Id = log.Id,
                Type = DetermineNotificationType(log.Action, log.EntityName),
                Title = FormatNotificationTitle(log.Action, log.EntityName),
                Message = log.Changes ?? "",
                Timestamp = log.Timestamp,
                Icon = GetNotificationIcon(log.Action, log.EntityName)
            }).ToList();

            logger.LogInformation("[Dashboard] ✅ Loaded {Count} notifications", notifications.Count);
            return notifications;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Dashboard] Error getting notifications");
            return new List<NotificationDto>();
        }
    }

    /// <summary>
    /// Récupère les événements récents
    /// </summary>
    public async Task<List<EventDto>> GetRecentEventsAsync(int count = 10)
    {
        try
        {
            var tenantId = tenantService.GetTenantId();
            var recentLogs = await context.AuditLogs
                .Where(a => a.TenantId == tenantId &&
                           (a.EntityName == "Route" || a.EntityName == "Delivery"))
                .OrderByDescending(a => a.Timestamp)
                .Take(Math.Min(count, 50))
                .ToListAsync();

            var events = recentLogs.Select(log => new EventDto
            {
                Id = log.Id,
                Title = FormatEventTitle(log.Action, log.EntityName, log.Changes),
                Type = DetermineEventType(log.Action),
                Timestamp = log.Timestamp
            }).ToList();

            logger.LogInformation("[Dashboard] ✅ Loaded {Count} events", events.Count);
            return events;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Dashboard] Error getting events");
            return new List<EventDto>();
        }
    }

    #endregion

    #region Charts

    /// <summary>
    /// Récupère les données du graphique Revenus + Livraisons
    /// </summary>
    public async Task<RevenueChartDataDto> GetRevenueChartDataAsync(ChartPeriod period)
    {
        try
        {
            var (startDate, labels) = GetPeriodRange(period);
            var endDate = GetEndDate(period, startDate);
            var chartData = new RevenueChartDataDto { Labels = labels };

            var allDeliveries = await context.Deliveries
                .Where(d => d.ScheduledDate >= startDate && d.ScheduledDate < endDate)
                .Select(d => new { d.ScheduledDate, d.Status, d.Price })
                .ToListAsync();

            for (int i = 0; i < labels.Count; i++)
            {
                var (periodStart, periodEnd) = GetPeriodDates(period, startDate, i);
                var bucket = allDeliveries
                    .Where(d => d.ScheduledDate >= periodStart && d.ScheduledDate < periodEnd)
                    .ToList();

                chartData.DeliveryCount.Add(bucket.Count);
                chartData.Revenues.Add((double)(bucket
                    .Where(d => d.Status == DeliveryStatus.Delivered)
                    .Sum(d => d.Price) / 1000m));
            }

            logger.LogInformation("[Dashboard] ✅ Revenue chart data loaded for {Period}", period);
            return chartData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Dashboard] Error getting revenue chart data");
            return new RevenueChartDataDto();
        }
    }

    /// <summary>
    /// Récupère les données du graphique Status
    /// </summary>
    public async Task<StatusChartDataDto> GetStatusChartDataAsync(ChartPeriod period)
    {
        try
        {
            var (startDate, _) = GetPeriodRange(period);
            var endDate = GetEndDate(period, startDate);

            var statusGroups = await context.Deliveries
                .Where(d => d.ScheduledDate >= startDate && d.ScheduledDate < endDate)
                .GroupBy(d => d.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var chartData = new StatusChartDataDto();

            // Ordre des statuts
            var orderedStatuses = new[]
            {
                DeliveryStatus.Delivered,
                DeliveryStatus.ToBePlanned,
                DeliveryStatus.Confirmed,
                DeliveryStatus.Canceled
            };

            foreach (var status in orderedStatuses)
            {
                var group = statusGroups.FirstOrDefault(g => g.Status == status);
                var count = group?.Count ?? 0;

                chartData.Labels.Add(GetStatusLabel(status));
                chartData.Values.Add(count);
            }

            chartData.DeliveredCount = statusGroups
                .FirstOrDefault(g => g.Status == DeliveryStatus.Delivered)?.Count ?? 0;
            chartData.TotalCount = statusGroups.Sum(g => g.Count);

            logger.LogInformation("[Dashboard] ✅ Status chart data loaded for {Period}", period);
            return chartData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Dashboard] Error getting status chart data");
            return new StatusChartDataDto();
        }
    }

    /// <summary>
    /// Récupère les données du graphique Magasins
    /// </summary>
    public async Task<StoreChartDataDto> GetStoreChartDataAsync(ChartPeriod period)
    {
        try
        {
            var (startDate, _) = GetPeriodRange(period);
            var endDate = GetEndDate(period, startDate);

            var storeGroups = await context.Deliveries
                .Where(d => d.ScheduledDate >= startDate &&
                           d.ScheduledDate < endDate &&
                           d.Status == DeliveryStatus.Delivered)
                .Include(d => d.Store)
                .GroupBy(d => d.Store.Name)
                .Select(g => new
                {
                    StoreName = g.Key,
                    Revenue = g.Sum(d => d.Price)
                })
                .OrderByDescending(g => g.Revenue)
                .Take(5) // Top 5 magasins
                .ToListAsync();

            var chartData = new StoreChartDataDto();

            foreach (var group in storeGroups)
            {
                chartData.StoreNames.Add(group.StoreName);
                chartData.Revenues.Add((double)(group.Revenue / 1000m)); // Convertir en k€
            }

            // Ajouter une catégorie "Autres" si nécessaire
            if (storeGroups.Count == 5)
            {
                var topStoresRevenue = storeGroups.Sum(g => g.Revenue);
                var totalRevenue = await context.Deliveries
                    .Where(d => d.ScheduledDate >= startDate &&
                               d.ScheduledDate < endDate &&
                               d.Status == DeliveryStatus.Delivered)
                    .SumAsync(d => d.Price);

                var othersRevenue = totalRevenue - topStoresRevenue;
                if (othersRevenue > 0)
                {
                    chartData.StoreNames.Add("Autres");
                    chartData.Revenues.Add((double)(othersRevenue / 1000m));
                }
            }

            logger.LogInformation("[Dashboard] ✅ Store chart data loaded for {Period}", period);
            return chartData;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Dashboard] Error getting store chart data");
            return new StoreChartDataDto();
        }
    }

    #endregion

    #region Helper Methods

    private (DateTime startDate, List<string> labels) GetPeriodRange(ChartPeriod period)
    {
        var today = DateTime.UtcNow.Date;
        return period switch
        {
            ChartPeriod.Week => (today.AddDays(-(int)today.DayOfWeek),
                new List<string> { "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam", "Dim" }),
            ChartPeriod.Month => (new DateTime(today.Year, today.Month, 1),
                new List<string> { "S1", "S2", "S3", "S4" }),
            ChartPeriod.Year => (new DateTime(today.Year, 1, 1),
                new List<string> { "Jan", "Fév", "Mar", "Avr", "Mai", "Jun", "Jul", "Aoû", "Sep", "Oct", "Nov", "Déc" }),
            _ => (today, new List<string>())
        };
    }

    private (DateTime start, DateTime end) GetPeriodDates(ChartPeriod period, DateTime startDate, int index)
    {
        return period switch
        {
            ChartPeriod.Week => (startDate.AddDays(index), startDate.AddDays(index + 1)),
            ChartPeriod.Month => (startDate.AddDays(index * 7), startDate.AddDays((index + 1) * 7)),
            ChartPeriod.Year => (startDate.AddMonths(index), startDate.AddMonths(index + 1)),
            _ => (startDate, startDate)
        };
    }

    private DateTime GetEndDate(ChartPeriod period, DateTime startDate)
    {
        return period switch
        {
            ChartPeriod.Week => startDate.AddDays(7),
            ChartPeriod.Month => startDate.AddMonths(1),
            ChartPeriod.Year => startDate.AddYears(1),
            _ => startDate
        };
    }

    private string GetStatusLabel(DeliveryStatus status) => status switch
    {
        DeliveryStatus.Delivered => "Livrées",
        DeliveryStatus.ToBePlanned => "À planifier",
        DeliveryStatus.Confirmed => "Confirmées",
        DeliveryStatus.InProgress => "En cours",
        DeliveryStatus.Canceled => "Annulées",
        _ => status.ToString()
    };

    private string DetermineNotificationType(string action, string entityName)
    {
        if (action.Contains("Created") || action.Contains("Completed")) return "Success";
        if (action.Contains("Failed") || action.Contains("Error")) return "Error";
        if (action.Contains("Warning") || action.Contains("Late")) return "Warning";
        return "Info";
    }

    private string FormatNotificationTitle(string action, string entityName)
    {
        return $"{action} - {entityName}";
    }

    private string GetNotificationIcon(string action, string entityName)
    {
        if (action.Contains("Created")) return "CheckCircle";
        if (action.Contains("Failed")) return "Error";
        if (action.Contains("Warning")) return "Warning";
        return "Info";
    }

    private string DetermineEventType(string action)
    {
        if (action.Contains("Completed") || action.Contains("Done")) return "Success";
        if (action.Contains("Failed") || action.Contains("Canceled")) return "Error";
        return "Info";
    }

    private string FormatEventTitle(string action, string entityName, string? details)
    {
        return !string.IsNullOrWhiteSpace(details) ? details : $"{action} - {entityName}";
    }

    #endregion
}