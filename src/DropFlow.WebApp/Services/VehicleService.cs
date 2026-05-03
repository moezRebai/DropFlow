using DropFlow.Shared.Common;
using DropFlow.Shared.Vehicles;
using DropFlow.WebApp.Interfaces;
using DropFlow.WebApp.Interfaces.Caches;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services;

/// <summary>
/// Service de gestion des véhicules avec cache et CRUD complet
/// </summary>
public class VehicleService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger<VehicleService> logger,
    ICacheService cacheService)
    : BaseApiService(httpClientFactory, localStorage, logger), IVehicleService
{
    // Clés de cache
    private const string CacheKeyAllVehicles = "vehicles_all";
    private const string CacheKeyVehiclePrefix = "vehicle_";

    // Durée de cache (10 minutes)
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    #region READ Operations

    /// <summary>
    /// Récupère tous les véhicules avec mise en cache
    /// </summary>
    public async Task<List<VehicleDto>> GetAllVehiclesAsync(bool forceRefresh = false)
    {
        if (!forceRefresh)
        {
            var cachedVehicles = cacheService.Get<List<VehicleDto>>(CacheKeyAllVehicles);
            if (cachedVehicles != null)
            {
                Logger.LogDebug("✅ Vehicles loaded from cache ({Count} items)", cachedVehicles.Count);
                return cachedVehicles;
            }
        }

        // Charger depuis l'API avec pagination max
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var result = await GetAsync<PagedResult<VehicleDto>>("/api/vehicles?page=1&pageSize=1000");

            if (result is { Items.Count: > 0 })
            {
                var vehicles = result.Items;

                // ✅ Mettre en cache
                cacheService.Set(CacheKeyAllVehicles, vehicles, CacheDuration);

                Logger.LogInformation("✅ Vehicles loaded from API in {ElapsedMs}ms ({Count} items)",
                    sw.ElapsedMilliseconds, vehicles.Count);

                return vehicles;
            }

            Logger.LogWarning("⚠️ No vehicles returned from API");
            return [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading vehicles from API");

            var expiredCache = cacheService.Get<List<VehicleDto>>(CacheKeyAllVehicles);

            if (expiredCache == null) return [];

            Logger.LogWarning("⚠️ Returning expired cache due to error ({Count} items)",
                expiredCache.Count);

            return expiredCache;
        }
    }

    /// <summary>
    /// Récupère les véhicules avec filtres (pagination server-side)
    /// </summary>
    public async Task<PagedResult<VehicleDto>> GetVehiclesAsync(VehicleFilterDto filter)
    {
        try
        {
            var queryString = $"?SearchTerm={Uri.EscapeDataString(filter.SearchTerm ?? string.Empty)}" +
                              $"&IsActive={filter.IsActive}" +
                              $"&Page={filter.Page}" +
                              $"&PageSize={filter.PageSize}";

            var result = await GetAsync<PagedResult<VehicleDto>>($"/api/vehicles{queryString}");

            if (result != null)
            {
                Logger.LogDebug("✅ Vehicles loaded with filters (Page {Page}, Total: {Total})",
                    filter.Page, result.TotalCount);
                return result;
            }

            Logger.LogWarning("⚠️ No paginated result from API");
            return new PagedResult<VehicleDto> { Items = [], TotalCount = 0 };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading vehicles with filters");
            return new PagedResult<VehicleDto> { Items = [], TotalCount = 0 };
        }
    }

    /// <summary>
    /// Récupère un véhicule par ID avec cache
    /// </summary>
    public async Task<VehicleDto?> GetVehicleByIdAsync(int id)
    {
        // Essayer d'abord dans le cache complet
        var cachedVehicles = cacheService.Get<List<VehicleDto>>(CacheKeyAllVehicles);
        if (cachedVehicles != null)
        {
            var vehicle = cachedVehicles.FirstOrDefault(v => v.Id == id);
            if (vehicle != null)
            {
                Logger.LogDebug("✅ Vehicle {Id} found in cache", id);
                return vehicle;
            }
        }

        // Essayer dans le cache individuel
        var cacheKey = $"{CacheKeyVehiclePrefix}{id}";
        var cachedVehicle = cacheService.Get<VehicleDto>(cacheKey);
        if (cachedVehicle != null)
        {
            Logger.LogDebug("✅ Vehicle {Id} found in individual cache", id);
            return cachedVehicle;
        }

        // Charger depuis l'API
        try
        {
            var response = await GetAsync<ResponseResult<VehicleDto>>($"/api/vehicles/{id}");

            if (response is { Succeeded: true, Data: not null })
            {
                cacheService.Set(cacheKey, response.Data, CacheDuration);
                Logger.LogDebug("✅ Vehicle {Id} loaded from API and cached", id);
                return response.Data;
            }

            Logger.LogWarning("⚠️ Vehicle {Id} not found", id);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading vehicle {Id} from API", id);
            return null;
        }
    }

    /// <summary>
    /// Vérifie si un véhicule est disponible pour une date donnée
    /// Utilise GetValueAsync pour lire une valeur primitive
    /// </summary>
    public async Task<bool> IsAvailableAsync(int vehicleId, DateTime date)
    {
        try
        {
            // Le controller retourne Ok(bool) qui est sérialisé comme une valeur simple
            var result = await GetValueAsync<bool>($"/api/vehicles/{vehicleId}/availability?date={date:yyyy-MM-dd}");

            Logger.LogDebug("✅ Vehicle {VehicleId} availability checked: {IsAvailable}",
                vehicleId, result);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error checking vehicle {VehicleId} availability", vehicleId);
            return false;
        }
    }

    /// <summary>
    /// Récupère les véhicules disponibles pour une date
    /// </summary>
    public async Task<List<VehicleDto>> GetAvailableVehiclesAsync(DateTime date)
    {
        try
        {
            var vehicles = await GetAsync<List<VehicleDto>>(
                $"/api/vehicles/available?date={date:yyyy-MM-dd}");

            Logger.LogDebug("✅ Found {Count} available vehicles for {Date:yyyy-MM-dd}",
                vehicles?.Count ?? 0, date);

            return vehicles ?? [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading available vehicles for {Date}", date);
            return [];
        }
    }

    #endregion

    #region CREATE Operation

    /// <summary>
    /// Crée un nouveau véhicule
    /// </summary>
    public async Task<ResponseResult> CreateVehicleAsync(CreateVehicleDto dto)
    {
        Logger.LogInformation("📝 Creating vehicle: {PlateNumber}", dto.PlateNumber);

        var response = await PostAsync("/api/vehicles", dto);

        if (response.Succeeded)
        {
            InvalidateCache();
        }

        return response;
    }

    #endregion

    #region UPDATE Operation

    /// <summary>
    /// Met à jour un véhicule existant
    /// </summary>
    public async Task<ResponseResult> UpdateVehicleAsync(int id, UpdateVehicleDto dto)
    {
        Logger.LogInformation("📝 Updating vehicle {Id}", id);

        var result = await PutAsync($"/api/vehicles/{id}", dto);

        if (result.Succeeded)
        {
            InvalidateVehicleCache(id);
        }

        return result;
    }

    #endregion

    #region DELETE Operation

    /// <summary>
    /// Supprime un véhicule (l'API gère le soft/hard delete)
    /// </summary>
    public async Task<ResponseResult> DeleteVehicleAsync(int id)
    {
        Logger.LogInformation("🗑️ Deleting vehicle {Id}", id);

        var result = await DeleteAsync($"/api/vehicles/{id}");

        if (result.Succeeded)
        {
            InvalidateVehicleCache(id);
        }

        return result;
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Invalide le cache des véhicules (après create/update/delete)
    /// </summary>
    public void InvalidateCache()
    {
        cacheService.Remove(CacheKeyAllVehicles);
        Logger.LogInformation("🗑️ Vehicles cache invalidated");
    }

    /// <summary>
    /// Invalide le cache d'un véhicule spécifique
    /// </summary>
    public void InvalidateVehicleCache(int id)
    {
        var cacheKey = $"{CacheKeyVehiclePrefix}{id}";
        cacheService.Remove(cacheKey);

        InvalidateCache();
        Logger.LogInformation("🗑️ Vehicle {Id} cache invalidated", id);
    }

    /// <summary>
    /// Force le rechargement depuis l'API
    /// </summary>
    public async Task<List<VehicleDto>> RefreshAsync()
    {
        InvalidateCache();
        return await GetAllVehiclesAsync(forceRefresh: true);
    }

    #endregion
}