using DropFlow.Shared.Common;
using DropFlow.Shared.Deliveries;
using DropFlow.Shared.Routes;
using DropFlow.WebApp.Interfaces;
using DropFlow.WebApp.Interfaces.Caches;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services;

/// <summary>
/// Service de gestion des feuilles de route avec cache et CRUD complet
/// </summary>
public class RouteService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger<RouteService> logger,
    ICacheService cacheService)
    : BaseApiService(httpClientFactory, localStorage, logger), IRouteService
{
    // Clés de cache
    private const string CacheKeyAllRoutes = "routes_all";
    private const string CacheKeyRoutePrefix = "route_";

    // Durée de cache (10 minutes)
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    #region READ Operations

    /// <summary>
    /// Récupère toutes les feuilles de route avec pagination et filtres
    /// </summary>
    public async Task<PagedResult<RouteViewDto>> GetRoutesAsync(RouteFilterDto filter)
    {
        try
        {
            var queryString = $"?Page={filter.Page}" +
                              $"&PageSize={filter.PageSize}";

            if (filter.Date.HasValue)
                queryString += $"&Date={filter.Date.Value:yyyy-MM-dd}";

            if (filter.Status.HasValue)
                queryString += $"&Status={filter.Status.Value}";

            if (filter.VehicleId.HasValue)
                queryString += $"&VehicleId={filter.VehicleId.Value}";

            if (filter.DriverId.HasValue)
                queryString += $"&DriverId={filter.DriverId.Value}";

            var result = await GetAsync<PagedResult<RouteViewDto>>($"/api/routes{queryString}");

            if (result != null)
            {
                Logger.LogDebug("✅ Routes loaded with filters (Page {Page}, Total: {Total})",
                    filter.Page, result.TotalCount);
                return result;
            }

            Logger.LogWarning("⚠️ No paginated result from API");
            return new PagedResult<RouteViewDto> { Items = [], TotalCount = 0 };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading route routes with filters");
            return new PagedResult<RouteViewDto> { Items = [], TotalCount = 0 };
        }
    }

    /// <summary>
    /// Récupère une feuille de route par ID avec cache
    /// </summary>
    public async Task<RouteDto?> GetRouteByIdAsync(int id)
    {
        // Essayer dans le cache individuel
        var cacheKey = $"{CacheKeyRoutePrefix}{id}";
        var cachedRoute = cacheService.Get<RouteDto>(cacheKey);
        if (cachedRoute != null)
        {
            Logger.LogDebug("✅ Route {Id} found in cache", id);
            return cachedRoute;
        }

        // Charger depuis l'API
        try
        {
            var result = await GetAsync<ResponseResult<RouteDto>>($"/api/routes/{id}");

            if (result == null || !result.Succeeded || result.Data == null)
            {
                Logger.LogWarning("⚠️ Route {Id} not found", id);
                return null;
            }

            cacheService.Set(cacheKey, result.Data, CacheDuration);
            Logger.LogDebug("✅ Route {Id} loaded from API and cached", id);

            return result.Data;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading route sheet {Id} from API", id);
            return null;
        }
    }

    /// <summary>
    /// Récupère les livraisons non assignées pour une date donnée
    /// </summary>
    public async Task<List<DeliveryDto>> GetUnassignedDeliveriesAsync(DateTime date)
    {
        try
        {
            var response = await GetAsync<ResponseResult<List<DeliveryDto>>>($"/api/deliveries/unassigned?date={date:yyyy-MM-dd}");
            return (response is { Succeeded: true } ? response.Data : []) ?? [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading unassigned deliveries");
            return [];
        }
    }

    #endregion

    #region CREATE & POST Operations

    /// <summary>
    /// Crée une nouvelle feuille de route complète (Wizard)
    /// </summary>
    public async Task<ResponseResult> CreateRouteAsync(CreateRouteDto dto)
    {
        Logger.LogInformation("📝 Creating route sheet for date: {Date}", dto.Date);

        var result = await PostAsync("/api/routes", dto);

        if (result.Succeeded)
        {
            InvalidateCache();
        }

        return result;
    }

    /// <summary>
    /// Optimise l'itinéraire avec Google Maps
    /// </summary>
    public async Task<ResponseResult<OptimizePathResponseDto>> OptimizeRouteAsync(OptimizePathRequestDto request)
    {
        Logger.LogInformation("🗺️ Optimizing route for {Count} deliveries", request.DeliveryIds.Count);

        var result = await PostWithResultAsync<OptimizePathRequestDto, OptimizePathResponseDto>("/api/routes/optimize", request);

        if (result is { Succeeded: true, Data: not null })
        {
            Logger.LogInformation("✅ Route optimized: {Distance}km, {Duration}min",
                result.Data.TotalDistanceKm, result.Data.TotalDurationMinutes);
        }

        return result;
    }

    public async Task<ResponseResult<OptimizePathResponseDto>> RecalculatePathMetricsAsync(OptimizePathRequestDto request)
    {
        Logger.LogInformation("🔄 Recalculating metrics (keeping manual order) for {Count} deliveries", 
            request.DeliveryIds.Count);

        var result = await PostWithResultAsync<OptimizePathRequestDto, OptimizePathResponseDto>(
            "/api/routes/recalculate-path", 
            request);

        if (result is { Succeeded: true, Data: not null })
        {
            Logger.LogInformation("✅ Metrics recalculated: {Distance}km, {Duration}min",
                result.Data.TotalDistanceKm, result.Data.TotalDurationMinutes);
        }

        return result;
    }
    
    /// <summary>
    /// Ajoute un membre d'équipe
    /// </summary>
    public async Task<ResponseResult> AddTeamMemberAsync(int routeId, TeamMemberDto dto)
    {
        Logger.LogInformation("👥 Adding team member {DriverId} to route {RouteId}", dto.DriverId, routeId);

        var result = await PostAsync($"/api/routes/{routeId}/team", dto);

        if (result.Succeeded)
        {
            InvalidateRouteCache(routeId);
        }

        return result;
    }

    /// <summary>
    /// Ajoute une livraison à la feuille de route
    /// </summary>
    public async Task<ResponseResult> AddDeliveryAsync(int routeId, int deliveryId)
    {
        Logger.LogInformation("📦 Adding delivery {DeliveryId} to route sheet {RouteId}", deliveryId, routeId);

        var result = await PostAsync($"/api/routes/{routeId}/deliveries/{deliveryId}");

        if (result.Succeeded)
        {
            InvalidateRouteCache(routeId);
        }

        return result;
    }

    /// <summary>
    /// Confirme la feuille de route (Draft → Confirmed)
    /// </summary>
    public async Task<ResponseResult> ConfirmAsync(int id)
    {
        Logger.LogInformation("✅ Confirming route sheet {Id}", id);

        var result = await PostAsync($"/api/routes/{id}/confirm");

        if (result.Succeeded)
        {
            InvalidateRouteCache(id);
        }

        return result;
    }

    /// <summary>
    /// Démarre la tournée (Confirmed → InProgress)
    /// </summary>
    public async Task<ResponseResult> StartAsync(int id)
    {
        Logger.LogInformation("▶️ Starting route sheet {Id}", id);

        var result = await PostAsync($"/api/routes/{id}/start");

        if (result.Succeeded)
        {
            InvalidateRouteCache(id);
        }

        return result;
    }

    /// <summary>
    /// Termine la tournée (InProgress → Completed)
    /// </summary>
    public async Task<ResponseResult> CompleteAsync(int id)
    {
        Logger.LogInformation("✔️ Completing route sheet {Id}", id);

        var result = await PostAsync($"/api/routes/{id}/complete");

        if (result.Succeeded)
        {
            InvalidateRouteCache(id);
        }

        return result;
    }

    /// <summary>
    /// Annule la feuille de route
    /// </summary>
    public async Task<ResponseResult> CancelAsync(int id)
    {
        Logger.LogInformation("❌ Cancelling route sheet {Id}", id);

        var result = await PostAsync($"/api/routes/{id}/cancel");

        if (result.Succeeded)
        {
            InvalidateRouteCache(id);
        }

        return result;
    }

    /// <summary>
    /// Recalcule les métriques (distance, durée)
    /// </summary>
    public async Task<ResponseResult> RecalculateMetricsAsync(int id)
    {
        Logger.LogInformation("🔄 Recalculating metrics for route sheet {Id}", id);

        var result = await PostAsync($"/api/routes/{id}/recalculate");

        if (result.Succeeded)
        {
            InvalidateRouteCache(id);
        }

        return result;
    }

    #endregion

    #region UPDATE Operations

    /// <summary>
    /// Met à jour l'heure de départ et l'adresse
    /// </summary>
    public async Task<ResponseResult> UpdateRouteAsync(int id, UpdateRouteDto dto)
    {
        Logger.LogInformation("📝 Updating route sheet {Id}", id);

        var result = await PutAsync($"/api/routes/{id}", dto);

        if (result.Succeeded)
        {
            InvalidateRouteCache(id);
        }

        return result;
    }

    /// <summary>
    /// Met à jour l'ordre des livraisons
    /// </summary>
    public async Task<ResponseResult> UpdateSequenceAsync(int id, List<UpdateDeliverySequenceDto> sequences)
    {
        Logger.LogInformation("🔄 Updating sequence for route sheet {Id}", id);

        var result = await PutAsync($"/api/routes/{id}/sequence", sequences);

        if (result.Succeeded)
        {
            InvalidateRouteCache(id);
        }

        return result;
    }

    #endregion

    #region DELETE Operations

    /// <summary>
    /// Supprime une feuille de route (seulement si Draft)
    /// </summary>
    public async Task<ResponseResult> DeleteRouteAsync(int id)
    {
        Logger.LogInformation("🗑️ Deleting route sheet {Id}", id);

        var result = await DeleteAsync($"/api/routes/{id}");

        if (result.Succeeded)
        {
            InvalidateRouteCache(id);
            InvalidateCache();
        }

        return result;
    }

    /// <summary>
    /// Retire un membre d'équipe
    /// </summary>
    public async Task<ResponseResult> RemoveTeamMemberAsync(int routeId, int driverId)
    {
        Logger.LogInformation("👥 Removing team member {DriverId} from route sheet {RouteId}", driverId, routeId);

        var result = await DeleteAsync($"/api/routes/{routeId}/team/{driverId}");

        if (result.Succeeded)
        {
            InvalidateRouteCache(routeId);
        }

        return result;
    }

    /// <summary>
    /// Retire une livraison de la feuille de route
    /// </summary>
    public async Task<ResponseResult> RemoveDeliveryAsync(int routeId, int deliveryId)
    {
        Logger.LogInformation("📦 Removing delivery {DeliveryId} from route sheet {RouteId}", deliveryId, routeId);

        var result = await DeleteAsync($"/api/routes/{routeId}/deliveries/{deliveryId}");

        if (result.Succeeded)
        {
            InvalidateRouteCache(routeId);
        }

        return result;
    }

    #endregion
    
    /// <summary>
    /// Télécharge la feuille de route PDF
    /// </summary>
    public async Task<(bool Success, byte[]? PdfBytes, string? ErrorMessage)> DownloadRouteSheetAsync(int routeId)
    {
        try
        {
            Logger.LogInformation("📄 Downloading route sheet PDF for route {RouteId}", routeId);

            var client = await CreateAuthorizedClientAsync();
            var response = await client.GetAsync($"/api/routes/{routeId}/download-sheet");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Logger.LogError("❌ Failed to download route sheet: Status {StatusCode}, Error: {Error}", 
                    response.StatusCode, error);
                return (false, null, $"Erreur {response.StatusCode}: {error}");
            }

            // Récupérer le PDF en bytes
            var pdfBytes = await response.Content.ReadAsByteArrayAsync();
        
            Logger.LogInformation("✅ Route sheet PDF downloaded successfully ({Size} bytes)", pdfBytes.Length);
        
            return (true, pdfBytes, null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error downloading route sheet PDF for route {RouteId}", routeId);
            return (false, null, ex.Message);
        }
    }
    
    #region Cache Management

    /// <summary>
    /// Invalide tout le cache des feuilles de route
    /// </summary>
    public void InvalidateCache()
    {
        cacheService.Remove(CacheKeyAllRoutes);
        Logger.LogInformation("🗑️ Routes cache invalidated");
    }

    /// <summary>
    /// Invalide le cache d'une feuille de route spécifique
    /// </summary>
    public void InvalidateRouteCache(int id)
    {
        var cacheKey = $"{CacheKeyRoutePrefix}{id}";
        cacheService.Remove(cacheKey);
        Logger.LogInformation("🗑️ Route {Id} cache invalidated", id);
    }

    #endregion
}