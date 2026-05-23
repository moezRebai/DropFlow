using DropFlow.Shared.Common;
using DropFlow.Shared.Drivers;
using DropFlow.WebApp.Interfaces;
using DropFlow.WebApp.Interfaces.Caches;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services;

public class DriverService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger<DriverService> logger,
    ICacheService cacheService)
    : BaseApiService(httpClientFactory, localStorage, logger), IDriverService
{
    private const string ApiEndpoint = "/api/drivers";
    private const string CacheKeyPrefix = "driver_";
    private const string CacheKeyActive = "drivers_active";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    #region READ Operations
    
    public async Task<PagedResult<DriverDto>> GetPagedAsync(DriverFilterDto filter)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={filter.Page}",
                $"pageSize={filter.PageSize}"
            };

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(filter.SearchTerm)}");

            if (filter.IsActive.HasValue)
                queryParams.Add($"isActive={filter.IsActive.Value}");

            var queryString = $"?{string.Join("&", queryParams)}";
            var result = await GetAsync<PagedResult<DriverDto>>($"{ApiEndpoint}{queryString}");

            Logger.LogDebug("✅ Loaded {Count} drivers (Page {Page}/{TotalPages})",
                result?.TotalCount ?? 0,
                result?.Page ?? 0,
                result?.TotalPages ?? 0);

            return result ?? new PagedResult<DriverDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading paged drivers");
            return new PagedResult<DriverDto>();
        }
    }

    /// <summary>
    /// Récupère tous les drivers sans pagination (pour usage interne)
    /// Utilise le cache
    /// </summary>
    public async Task<List<DriverDto>> GetAllAsync(bool forceRefresh = false)
    {
        var cacheKey = $"{CacheKeyPrefix}all";

        if (!forceRefresh)
        {
            var cached = cacheService.Get<List<DriverDto>>(cacheKey);
            if (cached != null)
            {
                Logger.LogDebug("✅ All drivers loaded from cache ({Count} items)", cached.Count);
                return cached;
            }
        }

        try
        {
            // Charger sans pagination (page=1, pageSize=1000)
            var result = await GetAsync<PagedResult<DriverDto>>($"{ApiEndpoint}?page=1&pageSize=1000");
            var drivers = result?.Items ?? [];

            if (drivers.Count <= 0) return drivers;
            cacheService.Set(cacheKey, drivers, CacheDuration);
            Logger.LogDebug("✅ All drivers loaded from API and cached ({Count} items)", drivers.Count);

            return drivers;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading all drivers");
            return [];
        }
    }
    
    public async Task<DriverDto?> GetByIdAsync(int id)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        // Essayer cache
        var cached = cacheService.Get<DriverDto>(cacheKey);
        if (cached != null)
        {
            Logger.LogDebug("✅ Driver {Id} loaded from cache", id);
            return cached;
        }

        try
        {
            var response = await GetAsync<ResponseResult<DriverDto>>($"{ApiEndpoint}/{id}");

            if (response?.Succeeded == true && response.Data != null)
            {
                // Mettre en cache
                cacheService.Set(cacheKey, response.Data, CacheDuration);
                Logger.LogDebug("✅ Driver {Id} loaded from API and cached", id);
                return response.Data;
            }

            Logger.LogWarning("⚠️ Driver {Id} not found", id);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading driver {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// GET /api/drivers/user/{userId}
    /// </summary>
    public async Task<DriverDto?> GetByUserIdAsync(string userId)
    {
        var response = await GetAsync<ResponseResult<DriverDto>>($"{ApiEndpoint}/user/{userId}");

        return response is not { Succeeded: true, Data: not null } ? null : response.Data;
    }

    /// <summary>
    /// GET /api/drivers/active
    /// </summary>
    public async Task<List<DriverDto>> GetActiveDriversAsync()
    {
        // Essayer cache
        var cached = cacheService.Get<List<DriverDto>>(CacheKeyActive);
        
        if (cached != null)
        {
            return cached;
        }

        try
        {
            var drivers = await GetAsync<List<DriverDto>>($"{ApiEndpoint}/active");

            if (drivers is not { Count: > 0 })
                return drivers ?? [];
            
            cacheService.Set(CacheKeyActive, drivers, CacheDuration);

            return drivers;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading active drivers");
            return [];
        }
    }

    /// <summary>
    /// GET /api/drivers/{id}/availability?date=yyyy-MM-dd
    /// Retourne DriverAvailabilityDto
    /// </summary>
    public async Task<DriverAvailabilityDto> CheckAvailabilityAsync(int driverId, DateTime date)
    {
        try
        {
            var availability = await GetAsync<DriverAvailabilityDto>(
                $"{ApiEndpoint}/{driverId}/availability?date={date:yyyy-MM-dd}");

            return availability ?? new DriverAvailabilityDto
            {
                DriverId = driverId,
                IsAvailable = false,
                ConflictReason = "Impossible de vérifier la disponibilité"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error checking driver {DriverId} availability", driverId);
            return new DriverAvailabilityDto
            {
                DriverId = driverId,
                IsAvailable = false,
                ConflictReason = ex.Message
            };
        }
    }

    /// <summary>
    /// POST /api/drivers/availability/check?date=yyyy-MM-dd
    /// </summary>
    public async Task<List<DriverAvailabilityDto>> CheckMultipleAvailabilityAsync(
        List<int> driverIds,
        DateTime date)
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var response = await client.PostAsJsonAsync(
                $"{ApiEndpoint}/availability/check?date={date:yyyy-MM-dd}",
                driverIds);

            if (response.IsSuccessStatusCode)
            {
                var availabilities = await response.Content.ReadFromJsonAsync<List<DriverAvailabilityDto>>();
                return availabilities ?? [];
            }

            Logger.LogWarning("⚠️ Check multiple availability failed: {StatusCode}", response.StatusCode);
            
            return [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error checking multiple drivers availability");
            return [];
        }
    }

    /// <summary>
    /// GET /api/drivers/available?date=yyyy-MM-dd
    /// </summary>
    public async Task<List<DriverDto>> GetAvailableDriversAsync(DateTime date)
    {
        try
        {
            var drivers = await GetAsync<List<DriverDto>>(
                $"{ApiEndpoint}/available?date={date:yyyy-MM-dd}");

            Logger.LogDebug("✅ Found {Count} available drivers for {Date:yyyy-MM-dd}",
                drivers?.Count ?? 0, date);

            return drivers ?? [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading available drivers for {Date}", date);
            return [];
        }
    }

    #endregion

    #region CREATE Operation

    /// <summary>
    /// POST /api/drivers
    /// Body: CreateDriverDto
    /// </summary>
    public async Task<ResponseResult> CreateAsync(CreateDriverDto dto)
    {
        Logger.LogInformation("📝 Creating driver for user {UserId}", dto.UserId);

        var response = await PostAsync(ApiEndpoint, dto);

        if (response.Succeeded)
        {
            InvalidateAllCaches();
        }

        return response;
    }

    #endregion

    #region UPDATE Operation

    /// <summary>
    /// PUT /api/drivers/{id}
    /// Body: UpdateDriverDto
    /// Retourne NoContent (204)
    /// </summary>
    public async Task<ResponseResult> UpdateAsync(int id, UpdateDriverDto dto)
    {
        try
        {
            Logger.LogInformation("📝 Updating driver {Id}", id);
            
            var result = await PutAsync($"/api/vehicles/{id}", dto);

            if (result.Succeeded)
            {
                InvalidateCacheById(id);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error updating driver {Id}", id);
            return ResponseResult.Failure(ex.Message);
        }
    }

    #endregion

    #region Cache Management

    private void InvalidateCacheById(int id)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";
        cacheService.Remove(cacheKey);
        cacheService.Remove($"{CacheKeyPrefix}all");
        cacheService.Remove(CacheKeyActive);
        Logger.LogDebug("🗑️ Cache invalidated for driver {Id}", id);
    }

    private void InvalidateAllCaches()
    {
        cacheService.Remove($"{CacheKeyPrefix}all");
        cacheService.Remove(CacheKeyActive);
        Logger.LogDebug("🗑️ All driver caches invalidated");
    }

    #endregion
}