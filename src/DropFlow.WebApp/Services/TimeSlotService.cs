using DropFlow.Shared.Common;
using DropFlow.Shared.TimeSlots;
using DropFlow.WebApp.Interfaces;
using DropFlow.WebApp.Interfaces.Caches;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services;

/// <summary>
/// Service de gestion des créneaux horaires (TimeSlots) avec cache et CRUD complet
/// </summary>
public class TimeSlotService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger<TimeSlotService> logger,
    ICacheService cacheService)
    : BaseApiService(httpClientFactory, localStorage, logger), ITimeSlotService
{
    // Clés de cache
    private const string CacheKeyAllTimeSlots = "timeslots_all";
    private const string CacheKeyTimeSlotPrefix = "timeslot_";
    
    // Durée de cache (10 minutes)
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    #region READ Operations

    /// <summary>
    /// Récupère tous les créneaux avec mise en cache
    /// </summary>
    public async Task<List<TimeSlotDto>> GetAllAsync(bool forceRefresh = false)
    {
        // ✅ Vérifier le cache d'abord (sauf si forceRefresh)
        if (!forceRefresh)
        {
            var cachedTimeSlots = cacheService.Get<List<TimeSlotDto>>(CacheKeyAllTimeSlots);
            if (cachedTimeSlots != null)
            {
                Logger.LogDebug("✅ TimeSlots loaded from cache ({Count} items)", cachedTimeSlots.Count);
                return cachedTimeSlots;
            }
        }

        // Charger depuis l'API
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var timeSlots = await GetAsync<List<TimeSlotDto>>("/api/timeslots");
            
            if (timeSlots is { Count: > 0 })
            {
                // ✅ Mettre en cache
                cacheService.Set(CacheKeyAllTimeSlots, timeSlots, CacheDuration);
                
                Logger.LogInformation("✅ TimeSlots loaded from API in {ElapsedMs}ms ({Count} items)", 
                    sw.ElapsedMilliseconds, timeSlots.Count);
                
                return timeSlots;
            }
            
            Logger.LogWarning("⚠️ No timeslots returned from API");
            return [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading timeslots from API");
            
            // Fallback : retourner cache expiré si disponible
            var expiredCache = cacheService.Get<List<TimeSlotDto>>(CacheKeyAllTimeSlots);
            if (expiredCache != null)
            {
                Logger.LogWarning("⚠️ Returning expired cache due to error ({Count} items)", 
                    expiredCache.Count);
                return expiredCache;
            }
            
            return [];
        }
    }

    /// <summary>
    /// Récupère un créneau par ID avec cache
    /// </summary>
    public async Task<TimeSlotDto?> GetByIdAsync(int id)
    {
        // Essayer d'abord dans le cache complet
        var cachedTimeSlots = cacheService.Get<List<TimeSlotDto>>(CacheKeyAllTimeSlots);
        if (cachedTimeSlots != null)
        {
            var timeSlot = cachedTimeSlots.FirstOrDefault(t => t.Id == id);
            if (timeSlot != null)
            {
                Logger.LogDebug("✅ TimeSlot {Id} found in cache", id);
                return timeSlot;
            }
        }

        // Essayer dans le cache individuel
        var cacheKey = $"{CacheKeyTimeSlotPrefix}{id}";
        var cachedTimeSlot = cacheService.Get<TimeSlotDto>(cacheKey);
        if (cachedTimeSlot != null)
        {
            Logger.LogDebug("✅ TimeSlot {Id} found in individual cache", id);
            return cachedTimeSlot;
        }

        // Charger depuis l'API
        try
        {
            var timeSlot = await GetAsync<TimeSlotDto>($"/api/timeslots/{id}");

            if (timeSlot == null) return timeSlot;
            
            cacheService.Set(cacheKey, timeSlot, CacheDuration);
            Logger.LogDebug("✅ TimeSlot {Id} loaded from API and cached", id);

            return timeSlot;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading timeslot {Id} from API", id);
            return null;
        }
    }

    #endregion

    #region CREATE Operation

    /// <summary>
    /// Crée un nouveau créneau
    /// </summary>
    public async Task<ResponseResult<int>> CreateAsync(CreateTimeSlotDto dto)
    {
        try
        {
            Logger.LogInformation("📝 Creating timeslot: {Name}", dto.Name);

            var client = await CreateAuthorizedClientAsync();
            var response = await client.PostAsJsonAsync("/api/timeslots", dto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ResponseResult<int>>();
                
                if (result is { Succeeded: true })
                {
                    // ✅ Invalider le cache
                    InvalidateCache();
                    
                    Logger.LogInformation("✅ TimeSlot created successfully with ID: {Id}", result.Data);
                    return result;
                }

                Logger.LogWarning("⚠️ TimeSlot creation returned unsuccessful result");
                return ResponseResult<int>.Failure("Échec de la création du créneau");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Logger.LogWarning("⚠️ Create timeslot failed. Status: {StatusCode}, Error: {Error}", 
                response.StatusCode, errorContent);
            
            return ResponseResult<int>.Failure(errorContent);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error creating timeslot");
            return ResponseResult<int>.Failure(ex.Message);
        }
    }

    #endregion

    #region UPDATE Operation

    /// <summary>
    /// Met à jour un créneau existant
    /// </summary>
    public async Task<ResponseResult> UpdateAsync(int id, UpdateTimeSlotDto dto)
    {
        try
        {
            Logger.LogInformation("📝 Updating timeslot {Id}: {Name}", id, dto.Name);

            var result = await PutAsync($"/api/timeslots/{id}", dto);

            if (result.Succeeded)
            {
                // ✅ Invalider le cache
                InvalidateTimeSlotCache(id);
                
                Logger.LogInformation("✅ TimeSlot {Id} updated successfully", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error updating timeslot {Id}", id);
            return ResponseResult.Failure(ex.Message);
        }
    }

    #endregion

    #region DELETE Operation

    /// <summary>
    /// Supprime un créneau
    /// </summary>
    public async Task<ResponseResult> DeleteAsync(int id)
    {
        try
        {
            Logger.LogInformation("🗑️ Deleting timeslot {Id}", id);

            var result = await DeleteAsync($"/api/timeslots/{id}");

            if (result.Succeeded)
            {
                // ✅ Invalider le cache
                InvalidateTimeSlotCache(id);
                
                Logger.LogInformation("✅ TimeSlot {Id} deleted successfully", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error deleting timeslot {Id}", id);
            return ResponseResult.Failure(ex.Message);
        }
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Invalide le cache des créneaux (après create/update/delete)
    /// </summary>
    public void InvalidateCache()
    {
        cacheService.Remove(CacheKeyAllTimeSlots);
        Logger.LogInformation("🗑️ TimeSlots cache invalidated");
    }

    /// <summary>
    /// Invalide le cache d'un créneau spécifique
    /// </summary>
    private void InvalidateTimeSlotCache(int id)
    {
        var cacheKey = $"{CacheKeyTimeSlotPrefix}{id}";
        cacheService.Remove(cacheKey);
        
        // Invalider aussi le cache complet car la liste a changé
        InvalidateCache();
        Logger.LogInformation("🗑️ TimeSlot {Id} cache invalidated", id);
    }

    /// <summary>
    /// Force le rechargement depuis l'API
    /// </summary>
    public async Task<List<TimeSlotDto>> RefreshAsync()
    {
        InvalidateCache();
        return await GetAllAsync(forceRefresh: true);
    }

    #endregion
}