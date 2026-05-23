using DropFlow.Application.Interfaces;
using DropFlow.Shared.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.API.Controllers;

/// <summary>
/// Contrôleur Dashboard - Statistiques et données du tableau de bord Manager
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    /// <summary>
    /// Récupère les statistiques KPI du dashboard
    /// </summary>
    /// <returns>Stats : Livraisons non planifiées, revenus mensuels, tournées actives, etc.</returns>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await dashboardService.GetStatsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Récupère les livraisons du jour
    /// </summary>
    /// <returns>Liste des livraisons planifiées aujourd'hui</returns>
    [HttpGet("today-deliveries")]
    public async Task<IActionResult> GetTodayDeliveries()
    {
        var deliveries = await dashboardService.GetTodayDeliveriesAsync();
        return Ok(deliveries);
    }

    /// <summary>
    /// Récupère les livraisons à risque (retard probable, 2e tentative, VIP, etc.)
    /// </summary>
    /// <returns>Liste des livraisons nécessitant une attention particulière</returns>
    [HttpGet("risky-deliveries")]
    public async Task<IActionResult> GetRiskyDeliveries()
    {
        var riskyDeliveries = await dashboardService.GetRiskyDeliveriesAsync();
        return Ok(riskyDeliveries);
    }

    /// <summary>
    /// Récupère les notifications récentes depuis les AuditLogs
    /// </summary>
    /// <param name="count">Nombre de notifications à retourner (défaut: 10, max: 50)</param>
    /// <returns>Liste des dernières notifications</returns>
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications([FromQuery] int count = 10)
    {
        if (count is < 1 or > 50)
            count = 10;

        var notifications = await dashboardService.GetNotificationsAsync(count);
        return Ok(notifications);
    }

    /// <summary>
    /// Récupère les événements récents (timeline)
    /// </summary>
    /// <param name="count">Nombre d'événements à retourner (défaut: 10, max: 50)</param>
    /// <returns>Liste des derniers événements</returns>
    [HttpGet("events")]
    public async Task<IActionResult> GetRecentEvents([FromQuery] int count = 10)
    {
        if (count is < 1 or > 50)
            count = 10;

        var events = await dashboardService.GetRecentEventsAsync(count);
        return Ok(events);
    }

    /// <summary>
    /// Récupère les données du graphique Revenus + Livraisons
    /// </summary>
    /// <param name="period">Période : Week, Month, Year</param>
    /// <returns>Labels et données pour le graphique en ligne</returns>
    [HttpGet("revenue-chart")]
    public async Task<IActionResult> GetRevenueChart([FromQuery] ChartPeriod period = ChartPeriod.Month)
    {
        var chartData = await dashboardService.GetRevenueChartDataAsync(period);
        return Ok(chartData);
    }

    /// <summary>
    /// Récupère les données du graphique Status des livraisons (Donut)
    /// </summary>
    /// <param name="period">Période : Week, Month, Year</param>
    /// <returns>Labels et valeurs pour le graphique donut</returns>
    [HttpGet("status-chart")]
    public async Task<IActionResult> GetStatusChart([FromQuery] ChartPeriod period = ChartPeriod.Month)
    {
        var chartData = await dashboardService.GetStatusChartDataAsync(period);
        return Ok(chartData);
    }

    /// <summary>
    /// Récupère les données du graphique Livraisons par magasin
    /// </summary>
    /// <param name="period">Période : Week, Month, Year</param>
    /// <returns>Noms des magasins et revenus correspondants</returns>
    [HttpGet("store-chart")]
    public async Task<IActionResult> GetStoreChart([FromQuery] ChartPeriod period = ChartPeriod.Month)
    {
        var chartData = await dashboardService.GetStoreChartDataAsync(period);
        return Ok(chartData);
    }
}
