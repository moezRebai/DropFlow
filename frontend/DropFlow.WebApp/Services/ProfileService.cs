using DropFlow.Shared.Common;
using DropFlow.Shared.Profil;
using DropFlow.WebApp.Interfaces;
using DropFlow.WebApp.Interfaces.Caches;
using DropFlow.WebApp.Models.Profile;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services;

/// <summary>
/// Service pour gérer le profil utilisateur avec cache
/// </summary>
public class ProfileService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger<ProfileService> logger,
    ICacheService cacheService)
    : BaseApiService(httpClientFactory, localStorage, logger), IProfileService
{
    // Clés de cache
    private const string CacheKeyProfile = "user_profile";
    private const string CacheKeyPreferences = "user_preferences";

    // Durée de cache
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    #region Profile

    /// <summary>
    /// Récupère le profil utilisateur avec cache
    /// </summary>
    public async Task<UserProfileDto?> GetProfileAsync()
    {
        // ✅ Essayer cache d'abord
        var cached = cacheService.Get<UserProfileDto>(CacheKeyProfile);
        if (cached != null)
        {
            Logger.LogDebug("✅ User profile loaded from cache");
            return cached;
        }

        // ❌ Cache miss - charger depuis API
        try
        {
            var profile = await GetAsync<UserProfileDto>("/api/profile");

            if (profile == null) return profile;
            // ✅ Mettre en cache
            cacheService.Set(CacheKeyProfile, profile, CacheDuration);
            Logger.LogDebug("✅ User profile loaded from API and cached");

            return profile;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading user profile");
            return null;
        }
    }

    /// <summary>
    /// Met à jour le profil utilisateur
    /// </summary>
    public async Task<ResponseResult> UpdateProfileAsync(UpdateProfileRequest request)
    {
        try
        {
            Logger.LogInformation("📝 Updating user profile");

            var result = await PutAsync("/api/profile", request);

            if (result.Succeeded)
            {
                // ✅ Invalider le cache
                InvalidateProfileCache();
                Logger.LogInformation("✅ User profile updated");
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error updating user profile");
            return ResponseResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Change le mot de passe utilisateur
    /// </summary>
    public async Task<ResponseResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            Logger.LogInformation("🔒 Changing user password");

            var result = await PutAsync("/api/profile/password", request);

            if (result.Succeeded)
            {
                Logger.LogInformation("✅ Password changed successfully");
                // Note : Pas besoin d'invalider le cache profil pour le mot de passe
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error changing password");
            return ResponseResult.Failure(ex.Message);
        }
    }

    #endregion

    #region Preferences

    /// <summary>
    /// Récupère les préférences utilisateur avec cache
    /// </summary>
    public async Task<UserPreferencesDto?> GetPreferencesAsync()
    {
        // ✅ Essayer cache d'abord
        var cached = cacheService.Get<UserPreferencesDto>(CacheKeyPreferences);
        if (cached != null)
        {
            Logger.LogDebug("✅ User preferences loaded from cache");
            return cached;
        }

        // ❌ Cache miss - charger depuis API
        try
        {
            var preferences = await GetAsync<UserPreferencesDto>("/api/profile/preferences");

            if (preferences != null)
            {
                // ✅ Mettre en cache
                cacheService.Set(CacheKeyPreferences, preferences, CacheDuration);
                Logger.LogDebug("✅ User preferences loaded from API and cached");
            }

            return preferences;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading user preferences");
            return null;
        }
    }

    /// <summary>
    /// Met à jour les préférences utilisateur
    /// </summary>
    public async Task<ResponseResult> UpdatePreferencesAsync(UserPreferencesDto request)
    {
        try
        {
            Logger.LogInformation("📝 Updating user preferences");

            var result = await PutAsync("/api/profile/preferences", request);

            if (result.Succeeded)
            {
                // ✅ Invalider le cache
                InvalidatePreferencesCache();
                Logger.LogInformation("✅ User preferences updated");
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error updating user preferences");
            return ResponseResult.Failure(ex.Message);
        }
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Invalide le cache du profil
    /// </summary>
    private void InvalidateProfileCache()
    {
        cacheService.Remove(CacheKeyProfile);
        Logger.LogInformation("🗑️ User profile cache invalidated");
    }

    /// <summary>
    /// Invalide le cache des préférences
    /// </summary>
    private void InvalidatePreferencesCache()
    {
        cacheService.Remove(CacheKeyPreferences);
        Logger.LogInformation("🗑️ User preferences cache invalidated");
    }

    /// <summary>
    /// Invalide tout le cache utilisateur (profil + préférences)
    /// </summary>
    public void InvalidateCache()
    {
        InvalidateProfileCache();
        InvalidatePreferencesCache();
        Logger.LogInformation("🗑️ All user cache invalidated");
    }

    #endregion
}