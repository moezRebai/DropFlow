using DropFlow.Shared.Common;
using DropFlow.Shared.Stores;
using DropFlow.WebApp.Interfaces;
using DropFlow.WebApp.Interfaces.Caches;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services;

/// <summary>
/// Service de gestion des enseignes (Stores) avec cache et CRUD complet
/// </summary>
public class StoreService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger<StoreService> logger,
    ICacheService cacheService)
    : BaseApiService(httpClientFactory, localStorage, logger), IStoreService
{
    // Clés de cache
    private const string CacheKeyAllStores = "stores_all";
    private const string CacheKeyStorePrefix = "store_";

    // Durée de cache (10 minutes)
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    #region READ Operations

    /// <summary>
    /// Récupère toutes les enseignes avec mise en cache
    /// </summary>
    public async Task<List<StoreDto>> GetAllStoresAsync(bool forceRefresh = false)
    {
        if (!forceRefresh)
        {
            var cachedStores = cacheService.Get<List<StoreDto>>(CacheKeyAllStores);
            if (cachedStores != null)
            {
                Logger.LogDebug("✅ Stores loaded from cache ({Count} items)", cachedStores.Count);
                return cachedStores;
            }
        }

        // Charger depuis l'API
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var stores = await GetAsync<List<StoreDto>>("/api/stores/all");

            if (stores is { Count: > 0 })
            {
                // ✅ Mettre en cache
                cacheService.Set(CacheKeyAllStores, stores, CacheDuration);

                Logger.LogInformation("✅ Stores loaded from API in {ElapsedMs}ms ({Count} items)",
                    sw.ElapsedMilliseconds, stores.Count);

                return stores;
            }

            Logger.LogWarning("⚠️ No stores returned from API");
            return [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading stores from API");

            var expiredCache = cacheService.Get<List<StoreDto>>(CacheKeyAllStores);
            
            if (expiredCache == null) return [];
            
            Logger.LogWarning("⚠️ Returning expired cache due to error ({Count} items)",
                expiredCache.Count);
                
            return expiredCache;

        }
    }

    /// <summary>
    /// Récupère les enseignes avec filtres (pagination server-side)
    /// </summary>
    public async Task<PagedResult<StoreDto>> GetStoresAsync(StoreFilterDto filter)
    {
        try
        {
            var queryString = $"?SearchTerm={filter.SearchTerm}" +
                              $"&IsActive={filter.IsActive}" +
                              $"&Page={filter.Page}" +
                              $"&PageSize={filter.PageSize}";

            var result = await GetAsync<PagedResult<StoreDto>>($"/api/stores{queryString}");

            if (result != null)
            {
                Logger.LogDebug("✅ Stores loaded with filters (Page {Page}, Total: {Total})",
                    filter.Page, result.TotalCount);
                return result;
            }

            Logger.LogWarning("⚠️ No paginated result from API");
            return new PagedResult<StoreDto> { Items = [], TotalCount = 0 };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading stores with filters");
            return new PagedResult<StoreDto> { Items = [], TotalCount = 0 };
        }
    }

    /// <summary>
    /// Récupère une enseigne par ID avec cache
    /// </summary>
    public async Task<StoreDto?> GetStoreByIdAsync(int id)
    {
        // Essayer d'abord dans le cache complet
        var cachedStores = cacheService.Get<List<StoreDto>>(CacheKeyAllStores);
        if (cachedStores != null)
        {
            var store = cachedStores.FirstOrDefault(s => s.Id == id);
            if (store != null)
            {
                Logger.LogDebug("✅ Store {Id} found in cache", id);
                return store;
            }
        }

        // Essayer dans le cache individuel
        var cacheKey = $"{CacheKeyStorePrefix}{id}";
        var cachedStore = cacheService.Get<StoreDto>(cacheKey);
        if (cachedStore != null)
        {
            Logger.LogDebug("✅ Store {Id} found in individual cache", id);
            return cachedStore;
        }

        // Charger depuis l'API
        try
        {
            var store = await GetAsync<StoreDto>($"/api/stores/{id}");

            if (store == null) return store;

            cacheService.Set(cacheKey, store, CacheDuration);
            Logger.LogDebug("✅ Store {Id} loaded from API and cached", id);

            return store;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading store {Id} from API", id);
            return null;
        }
    }

    /// <summary>
    /// Récupère la liste des enseignes pour lookup/dropdown
    /// </summary>
    public async Task<List<StoreLookupDto>> GetStoresLookupAsync()
    {
        try
        {
            var stores = await GetAsync<List<StoreLookupDto>>("/api/stores/lookup");
            return stores ?? [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading stores lookup");
            return [];
        }
    }

    #endregion

    #region CREATE Operation

    /// <summary>
    /// Crée une nouvelle enseigne
    /// </summary>
    public async Task<ResponseResult> CreateStoreAsync(CreateStoreDto dto)
    {
        Logger.LogInformation("📝 Creating store: {Name}", dto.Name);

        var response = await PostAsync("/api/stores", dto);

        if (response.Succeeded)
        {
            InvalidateCache();
        }

        return response;
    }

    #endregion

    #region UPDATE Operation

    /// <summary>
    /// Met à jour une enseigne existante
    /// </summary>
    public async Task<ResponseResult> UpdateStoreAsync(int id, UpdateStoreDto dto)
    {
        Logger.LogInformation("📝 Updating store {Id}: {Name}", id, dto.Name);

        var result = await PutAsync($"/api/stores/{id}", dto);

        if (result.Succeeded)
        {
            InvalidateStoreCache(id);
        }

        return result;
    }

    #endregion

    #region DELETE Operation

    /// <summary>
    /// Supprime une enseigne (l'API gère le soft/hard delete)
    /// </summary>
    public async Task<ResponseResult> DeleteStoreAsync(int id)
    {
        Logger.LogInformation("🗑️ Deleting store {Id}", id);

        var result = await DeleteAsync($"/api/stores/{id}");

        if (result.Succeeded)
        {
            InvalidateStoreCache(id);
        }

        return result;
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Invalide le cache des enseignes (après create/update/delete)
    /// </summary>
    public void InvalidateCache()
    {
        cacheService.Remove(CacheKeyAllStores);
        Logger.LogInformation("🗑️ Stores cache invalidated");
    }

    /// <summary>
    /// Invalide le cache d'une enseigne spécifique
    /// </summary>
    public void InvalidateStoreCache(int id)
    {
        var cacheKey = $"{CacheKeyStorePrefix}{id}";
        cacheService.Remove(cacheKey);

        InvalidateCache();
        Logger.LogInformation("🗑️ Store {Id} cache invalidated", id);
    }

    /// <summary>
    /// Force le rechargement depuis l'API
    /// </summary>
    public async Task<List<StoreDto>> RefreshAsync()
    {
        InvalidateCache();
        return await GetAllStoresAsync(forceRefresh: true);
    }

    #endregion
}