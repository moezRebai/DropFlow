using DropFlow.Shared.Common;
using DropFlow.Shared.Tenants;
using DropFlow.Shared.Tenants.Depots;
using DropFlow.WebApp.Interfaces;
using DropFlow.WebApp.Interfaces.Caches;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services;

/// <summary>
/// Service de gestion des paramètres entreprise et dépôts avec cache
/// </summary>
public class TenantManagementService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger<TenantManagementService> logger,
    ICacheService cacheService)
    : BaseApiService(httpClientFactory, localStorage, logger), ITenantManagementService
{
    // Clés de cache
    private const string CacheKeyTenant = "tenant_current";
    private const string CacheKeyAllDepots = "depots_all";
    private const string CacheKeyDepotPrefix = "depot_";

    // Durée de cache (10 minutes)
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    // ═══════════════════════════════════════════════════════════
    // TENANT INFO
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Récupère les informations du tenant courant avec mise en cache
    /// </summary>
    public async Task<TenantDto?> GetCurrentTenantAsync(bool forceRefresh = false)
    {
        if (!forceRefresh)
        {
            var cachedTenant = cacheService.Get<TenantDto>(CacheKeyTenant);
            if (cachedTenant != null)
            {
                Logger.LogDebug("✅ Tenant loaded from cache");
                return cachedTenant;
            }
        }

        // Charger depuis l'API
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var tenant = await GetAsync<TenantDto>("/api/tenants/current");

            if (tenant != null)
            {
                // ✅ Mettre en cache
                cacheService.Set(CacheKeyTenant, tenant, CacheDuration);

                Logger.LogInformation("✅ Tenant loaded from API in {ElapsedMs}ms", sw.ElapsedMilliseconds);

                return tenant;
            }

            Logger.LogWarning("⚠️ No tenant returned from API");
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading tenant from API");

            var expiredCache = cacheService.Get<TenantDto>(CacheKeyTenant);
            
            if (expiredCache != null)
            {
                Logger.LogWarning("⚠️ Returning expired cache due to error");
            }
                
            return expiredCache;
        }
    }

    /// <summary>
    /// Met à jour les informations générales de l'entreprise
    /// </summary>
    public async Task<ResponseResult> UpdateCompanyInfoAsync(UpdateTenantCompanyInfoDto dto)
    {
        Logger.LogInformation("📝 Updating company info: {CompanyName}", dto.CompanyName);

        var result = await PutAsync("/api/tenants/company-info", dto);

        if (result.Succeeded)
        {
            InvalidateTenantCache();
        }

        return result;
    }

    /// <summary>
    /// Met à jour les informations légales
    /// </summary>
    public async Task<ResponseResult> UpdateLegalInfoAsync(UpdateTenantLegalInfoDto dto)
    {
        Logger.LogInformation("📝 Updating legal info (SIRET: {Siret})", dto.Siret);

        var result = await PutAsync("/api/tenants/legal-info", dto);

        if (result.Succeeded)
        {
            InvalidateTenantCache();
        }

        return result;
    }

    /// <summary>
    /// Met à jour le logo de l'entreprise
    /// </summary>
    public async Task<ResponseResult> UpdateLogoAsync(UpdateTenantLogoDto dto)
    {
        Logger.LogInformation("📝 Updating logo: {LogoUrl}", dto.LogoUrl);

        var result = await PutAsync("/api/tenants/logo", dto);

        if (result.Succeeded)
        {
            InvalidateTenantCache();
        }

        return result;
    }

    /// <summary>
    /// Supprime le logo de l'entreprise
    /// </summary>
    public async Task<ResponseResult> RemoveLogoAsync()
    {
        Logger.LogInformation("🗑️ Removing logo");

        var result = await DeleteAsync("/api/tenants/logo");

        if (result.Succeeded)
        {
            InvalidateTenantCache();
        }

        return result;
    }

    // ═══════════════════════════════════════════════════════════
    // DEPOTS
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Récupère tous les dépôts avec mise en cache
    /// </summary>
    public async Task<List<TenantDepotDto>> GetAllDepotsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh)
        {
            var cachedDepots = cacheService.Get<List<TenantDepotDto>>(CacheKeyAllDepots);
            if (cachedDepots != null)
            {
                Logger.LogDebug("✅ Depots loaded from cache ({Count} items)", cachedDepots.Count);
                return cachedDepots;
            }
        }

        // Charger depuis l'API
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var depots = await GetAsync<List<TenantDepotDto>>("/api/tenants/depots/all");

            if (depots is { Count: > 0 })
            {
                // ✅ Mettre en cache
                cacheService.Set(CacheKeyAllDepots, depots, CacheDuration);

                Logger.LogInformation("✅ Depots loaded from API in {ElapsedMs}ms ({Count} items)",
                    sw.ElapsedMilliseconds, depots.Count);

                return depots;
            }

            Logger.LogWarning("⚠️ No depots returned from API");
            return [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading depots from API");

            var expiredCache = cacheService.Get<List<TenantDepotDto>>(CacheKeyAllDepots);
            
            if (expiredCache == null) return [];
            
            Logger.LogWarning("⚠️ Returning expired cache due to error ({Count} items)",
                expiredCache.Count);
                
            return expiredCache;
        }
    }

    /// <summary>
    /// Récupère les dépôts avec filtres (pagination server-side)
    /// </summary>
    public async Task<PagedResult<TenantDepotDto>> GetDepotsAsync(TenantDepotFilterDto filter)
    {
        try
        {
            var queryString = $"?SearchTerm={filter.SearchTerm}" +
                              $"&IsActive={filter.IsActive}" +
                              $"&IsDefault={filter.IsDefault}" +
                              $"&City={filter.City}" +
                              $"&ZipCode={filter.ZipCode}" +
                              $"&Page={filter.Page}" +
                              $"&PageSize={filter.PageSize}";

            var result = await GetAsync<PagedResult<TenantDepotDto>>($"/api/tenants/depots{queryString}");

            if (result != null)
            {
                Logger.LogDebug("✅ Depots loaded with filters (Page {Page}, Total: {Total})",
                    filter.Page, result.TotalCount);
                return result;
            }

            Logger.LogWarning("⚠️ No paginated result from API");
            return new PagedResult<TenantDepotDto> { Items = [], TotalCount = 0 };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading depots with filters");
            return new PagedResult<TenantDepotDto> { Items = [], TotalCount = 0 };
        }
    }

    /// <summary>
    /// Récupère un dépôt par ID avec cache
    /// </summary>
    public async Task<TenantDepotDto?> GetDepotByIdAsync(int id)
    {
        // Essayer d'abord dans le cache complet
        var cachedDepots = cacheService.Get<List<TenantDepotDto>>(CacheKeyAllDepots);
        if (cachedDepots != null)
        {
            var depot = cachedDepots.FirstOrDefault(d => d.Id == id);
            if (depot != null)
            {
                Logger.LogDebug("✅ Depot {Id} found in cache", id);
                return depot;
            }
        }

        // Essayer dans le cache individuel
        var cacheKey = $"{CacheKeyDepotPrefix}{id}";
        var cachedDepot = cacheService.Get<TenantDepotDto>(cacheKey);
        if (cachedDepot != null)
        {
            Logger.LogDebug("✅ Depot {Id} found in individual cache", id);
            return cachedDepot;
        }

        // Charger depuis l'API
        try
        {
            var depot = await GetAsync<TenantDepotDto>($"/api/tenants/depots/{id}");

            if (depot == null) return depot;

            cacheService.Set(cacheKey, depot, CacheDuration);
            Logger.LogDebug("✅ Depot {Id} loaded from API and cached", id);

            return depot;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading depot {Id} from API", id);
            return null;
        }
    }

    /// <summary>
    /// Crée un nouveau dépôt
    /// </summary>
    public async Task<ResponseResult> CreateDepotAsync(CreateTenantDepotDto dto)
    {
        Logger.LogInformation("📝 Creating depot: {Name}", dto.Name);

        var response = await PostAsync("/api/tenants/depots", dto);

        if (response.Succeeded)
        {
            InvalidateDepotsCache();
        }

        return response;
    }

    /// <summary>
    /// Met à jour un dépôt existant
    /// </summary>
    public async Task<ResponseResult> UpdateDepotAsync(int id, UpdateTenantDepotDto dto)
    {
        Logger.LogInformation("📝 Updating depot {Id}: {Name}", id, dto.Name);

        var result = await PutAsync($"/api/tenants/depots/{id}", dto);

        if (result.Succeeded)
        {
            InvalidateDepotCache(id);
        }

        return result;
    }

    /// <summary>
    /// Supprime un dépôt
    /// </summary>
    public async Task<ResponseResult> DeleteDepotAsync(int id)
    {
        Logger.LogInformation("🗑️ Deleting depot {Id}", id);

        var result = await DeleteAsync($"/api/tenants/depots/{id}");

        if (result.Succeeded)
        {
            InvalidateDepotCache(id);
        }

        return result;
    }

    /// <summary>
    /// Définit un dépôt comme par défaut
    /// </summary>
    public async Task<ResponseResult> SetDefaultDepotAsync(int id)
    {
        Logger.LogInformation("⭐ Setting depot {Id} as default", id);

        var result = await PostAsync($"/api/tenants/depots/{id}/set-default");

        if (result.Succeeded)
        {
            InvalidateDepotsCache();
        }

        return result;
    }

    /// <summary>
    /// Active/Désactive un dépôt
    /// </summary>
    public async Task<ResponseResult> ToggleDepotStatusAsync(int id)
    {
        Logger.LogInformation("🔄 Toggling depot {Id} status", id);

        var result = await PostAsync($"/api/tenants/depots/{id}/toggle-status");

        if (result.Succeeded)
        {
            InvalidateDepotCache(id);
        }

        return result;
    }

    // ═══════════════════════════════════════════════════════════
    // CACHE MANAGEMENT
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Invalide tout le cache (tenant + depots)
    /// </summary>
    public void InvalidateCache()
    {
        cacheService.Remove(CacheKeyTenant);
        cacheService.Remove(CacheKeyAllDepots);
        Logger.LogInformation("🗑️ Tenant and Depots cache invalidated");
    }

    /// <summary>
    /// Invalide uniquement le cache du tenant
    /// </summary>
    private void InvalidateTenantCache()
    {
        cacheService.Remove(CacheKeyTenant);
        Logger.LogInformation("🗑️ Tenant cache invalidated");
    }

    /// <summary>
    /// Invalide uniquement le cache des dépôts
    /// </summary>
    private void InvalidateDepotsCache()
    {
        cacheService.Remove(CacheKeyAllDepots);
        Logger.LogInformation("🗑️ Depots cache invalidated");
    }

    /// <summary>
    /// Invalide le cache d'un dépôt spécifique
    /// </summary>
    public void InvalidateDepotCache(int id)
    {
        var cacheKey = $"{CacheKeyDepotPrefix}{id}";
        cacheService.Remove(cacheKey);

        InvalidateDepotsCache();
        Logger.LogInformation("🗑️ Depot {Id} cache invalidated", id);
    }

    /// <summary>
    /// Force le rechargement du tenant depuis l'API
    /// </summary>
    public async Task<TenantDto?> RefreshTenantAsync()
    {
        InvalidateTenantCache();
        return await GetCurrentTenantAsync(forceRefresh: true);
    }

    /// <summary>
    /// Force le rechargement des dépôts depuis l'API
    /// </summary>
    public async Task<List<TenantDepotDto>> RefreshDepotsAsync()
    {
        InvalidateDepotsCache();
        return await GetAllDepotsAsync(forceRefresh: true);
    }
}