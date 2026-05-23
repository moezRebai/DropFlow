using DropFlow.Application.Common;
using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Shared.Enums;
using DropFlow.Shared.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Application.Services;

public class DashboardService(
    IApplicationDbContext context,
    ITenantService tenantService,
    IAppCacheService cache,
    ILogger<DashboardService> logger)
    : IDashboardService
{
    #region Stats KPI

    public Task<DashboardStatsDto> GetStatsAsync()
    {
        var tenantId = tenantService.GetTenantId();
        return cache.GetOrSetAsync(
            CacheKeys.DashboardStats(tenantId),
            FetchStatsAsync,
            TimeSpan.FromMinutes(2));
    }

    private async Task<DashboardStatsDto> FetchStatsAsync()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);
            var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var endOfThisMonth = startOfMonth.AddMonths(1);

            var unplannedCount = await context.Deliveries
                .Where(d => d.Status == DeliveryStatus.ToBePlanned)
                .CountAsync();

            var unplannedYesterday = await context.Deliveries
                .Where(d => d.Status == DeliveryStatus.ToBePlanned &&
                           d.CreatedDate.Date == yesterday)
                .CountAsync();

            var todayDeliveriesCount = await context.Deliveries
                .CountAsync(d => d.ScheduledDate == today);
            var deliveredTodayCount = await context.Deliveries
                .CountAsync(d => d.ScheduledDate == today && d.Status == DeliveryStatus.Delivered);

            var monthlyRevenue = await context.Deliveries
                .Where(d => d.ScheduledDate >= startOfMonth &&
                           d.ScheduledDate < endOfThisMonth &&
                           d.Status == DeliveryStatus.Delivered)
                .SumAsync(d => d.Price);
            var lastMonthRevenue = await context.Deliveries
                .Where(d => d.ScheduledDate >= startOfLastMonth &&
                           d.ScheduledDate < startOfMonth &&
                           d.Status == DeliveryStatus.Delivered)
                .SumAsync(d => d.Price);

            var revenueTrend = lastMonthRevenue > 0
                ? ((monthlyRevenue - lastMonthRevenue) / lastMonthRevenue) * 100
                : 0;

            var totalRoutesToday = await context.Routes
                .CountAsync(r => r.Date == today);
            var activeRoutesCount = await context.Routes
                .CountAsync(r => r.Date == today &&
                    (r.Status == RouteStatus.InProgress || r.Status == RouteStatus.Confirmed));

            var driversOnRoad = await context.RouteTeams
                .Where(rt => rt.Route.Date == today && rt.Route.Status == RouteStatus.InProgress)
                .Select(rt => rt.DriverId)
                .Distinct()
                .CountAsync();

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

    public Task<List<TodayDeliveryDto>> GetTodayDeliveriesAsync()
    {
        var tenantId = tenantService.GetTenantId();
        return cache.GetOrSetAsync(
            CacheKeys.DashboardTodayDeliveries(tenantId),
            FetchTodayDeliveriesAsync,
            TimeSpan.FromMinutes(5));
    }

    private async Task<List<TodayDeliveryDto>> FetchTodayDeliveriesAsync()
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
            return [];
        }
    }

    #endregion

    #region Risky Deliveries

    public Task<List<RiskyDeliveryDto>> GetRiskyDeliveriesAsync()
    {
        var tenantId = tenantService.GetTenantId();
        return cache.GetOrSetAsync(
            CacheKeys.DashboardRisky(tenantId),
            FetchRiskyDeliveriesAsync,
            TimeSpan.FromMinutes(10));
    }

    private async Task<List<RiskyDeliveryDto>> FetchRiskyDeliveriesAsync()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var threeDaysAgo = today.AddDays(-3);

            var rows = await context.Deliveries
                .Where(d => d.Status == DeliveryStatus.ToBePlanned &&
                           d.CreatedDate.Date <= threeDaysAgo)
                .Include(d => d.Client)
                .Include(d => d.ClientAddress)
                .Include(d => d.TimeSlot)
                .OrderBy(d => d.CreatedDate)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            var riskyDeliveries = rows.Select(d => new RiskyDeliveryDto
            {
                Id = d.Id,
                Reference = d.Reference,
                ClientName = d.Client.DisplayName,
                DeliveryAddress = d.ClientAddress.Address,
                DeliveryCity = d.ClientAddress.City,
                EstimatedTime = d.ScheduledDate.HasValue
                    ? d.ScheduledDate.Value.Add(d.TimeSlot != null ? d.TimeSlot.StartTime : TimeSpan.Zero)
                    : DateTime.UtcNow,
                RiskReason = CalculateRiskReason(d.CreatedDate),
                RiskLevel = CalculateRiskLevel(d.CreatedDate)
            }).ToList();

            logger.LogInformation("[Dashboard] ✅ Loaded {Count} risky deliveries", riskyDeliveries.Count);
            return riskyDeliveries;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Dashboard] Error getting risky deliveries");
            return [];
        }
    }

    #endregion

    #region Notifications & Events

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
            return [];
        }
    }

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
            return [];
        }
    }

    #endregion

    #region Charts

    public Task<RevenueChartDataDto> GetRevenueChartDataAsync(ChartPeriod period)
    {
        var tenantId = tenantService.GetTenantId();
        return cache.GetOrSetAsync(
            CacheKeys.DashboardRevenue(tenantId, period.ToString()),
            () => FetchRevenueChartDataAsync(period),
            TimeSpan.FromMinutes(15));
    }

    private async Task<RevenueChartDataDto> FetchRevenueChartDataAsync(ChartPeriod period)
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

    public Task<StatusChartDataDto> GetStatusChartDataAsync(ChartPeriod period)
    {
        var tenantId = tenantService.GetTenantId();
        return cache.GetOrSetAsync(
            CacheKeys.DashboardStatus(tenantId, period.ToString()),
            () => FetchStatusChartDataAsync(period),
            TimeSpan.FromMinutes(15));
    }

    private async Task<StatusChartDataDto> FetchStatusChartDataAsync(ChartPeriod period)
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
                chartData.Labels.Add(GetStatusLabel(status));
                chartData.Values.Add(group?.Count ?? 0);
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

    public Task<StoreChartDataDto> GetStoreChartDataAsync(ChartPeriod period)
    {
        var tenantId = tenantService.GetTenantId();
        return cache.GetOrSetAsync(
            CacheKeys.DashboardStoreChart(tenantId, period.ToString()),
            () => FetchStoreChartDataAsync(period),
            TimeSpan.FromMinutes(30));
    }

    private async Task<StoreChartDataDto> FetchStoreChartDataAsync(ChartPeriod period)
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
                .Take(5)
                .ToListAsync();

            var chartData = new StoreChartDataDto();

            foreach (var group in storeGroups)
            {
                chartData.StoreNames.Add(group.StoreName);
                chartData.Revenues.Add((double)(group.Revenue / 1000m));
            }

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
        DeliveryStatus.Delivered    => "Livrées",
        DeliveryStatus.ToBePlanned  => "À planifier",
        DeliveryStatus.Confirmed    => "Confirmées",
        DeliveryStatus.InProgress   => "En cours",
        DeliveryStatus.Canceled     => "Annulées",
        _ => status.ToString()
    };

    private static string CalculateRiskReason(DateTime createdDate)
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

    private static string CalculateRiskLevel(DateTime createdDate)
    {
        var daysOld = (DateTime.UtcNow.Date - createdDate.Date).Days;
        return daysOld switch
        {
            >= 7 => "Error",
            >= 5 => "Warning",
            _    => "Info"
        };
    }

    private string DetermineNotificationType(string action, string entityName)
    {
        if (action.Contains("Created") || action.Contains("Completed")) return "Success";
        if (action.Contains("Failed") || action.Contains("Error")) return "Error";
        if (action.Contains("Warning") || action.Contains("Late")) return "Warning";
        return "Info";
    }

    private string FormatNotificationTitle(string action, string entityName) => $"{action} - {entityName}";

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

    private string FormatEventTitle(string action, string entityName, string? details) =>
        !string.IsNullOrWhiteSpace(details) ? details : $"{action} - {entityName}";

    #endregion
}
