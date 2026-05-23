using DropFlow.Shared.Clients;
using DropFlow.Shared.Common;
using DropFlow.Shared.Deliveries;
using DropFlow.WebApp.Interfaces;
using DropFlow.WebApp.Interfaces.Caches;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services;

/// <summary>
/// Service de gestion des clients côté frontend avec cache et CRUD complet
/// </summary>
public class ClientService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger<ClientService> logger,
    ICacheService cacheService)
    : BaseApiService(httpClientFactory, localStorage, logger), IClientService
{
    // Clés de cache
    private const string CacheKeyClientPrefix = "client_";
    private const string CacheKeySearchPrefix = "client_search_";
    private const string CacheKeyDeliveriesPrefix = "client_deliveries_";
    
    // Durée de cache
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan SearchCacheDuration = TimeSpan.FromMinutes(5);

    #region READ Operations

    /// <summary>
    /// Récupère la liste complète des clients avec pagination et filtres
    /// </summary>
    public async Task<PagedResult<ClientDto>> GetClientsAsync(ClientFilterDto filter)
    {
        try
        {
            var queryString = $"?SearchTerm={Uri.EscapeDataString(filter.SearchTerm ?? "")}" +
                            $"&Page={filter.Page}" +
                            $"&PageSize={filter.PageSize}";

            var result = await GetAsync<PagedResult<ClientDto>>($"/api/clients{queryString}");
            
            if (result != null)
            {
                Logger.LogDebug("✅ Clients loaded (Page {Page}, Total: {Total})", 
                    filter.Page, result.TotalCount);
                return result;
            }

            Logger.LogWarning("⚠️ No paginated result from API");
            return new PagedResult<ClientDto> { Items = [], TotalCount = 0 };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading clients with filters");
            return new PagedResult<ClientDto> { Items = [], TotalCount = 0 };
        }
    }

    /// <summary>
    /// Récupère un client par ID avec cache
    /// </summary>
    public async Task<ClientDto?> GetClientByIdAsync(int id)
    {
        var cacheKey = $"{CacheKeyClientPrefix}{id}";

        // ✅ Vérifier le cache
        var cachedClient = cacheService.Get<ClientDto>(cacheKey);
        if (cachedClient != null)
        {
            Logger.LogDebug("✅ Client {Id} found in cache", id);
            return cachedClient;
        }

        try
        {
            var client = await GetAsync<ClientDto>($"/api/clients/{id}");

            if (client == null)
            {
                Logger.LogWarning("⚠️ Client {Id} not found", id);
                return null;
            }
            
            // ✅ Mettre en cache
            cacheService.Set(cacheKey, client, CacheDuration);
            Logger.LogDebug("✅ Client {Id} loaded from API and cached", id);

            return client;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading client {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Recherche des clients (autocomplete) avec cache
    /// </summary>
    public async Task<List<ClientLookupDto>> SearchClientsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            Logger.LogWarning("⚠️ Empty search term");
            return [];
        }

        var normalizedSearchTerm = searchTerm.Trim().ToLowerInvariant();
        var cacheKey = $"{CacheKeySearchPrefix}{normalizedSearchTerm}";

        // ✅ Vérifier le cache
        var cachedResults = cacheService.Get<List<ClientLookupDto>>(cacheKey);
        if (cachedResults != null)
        {
            Logger.LogDebug("✅ Client search cached for '{SearchTerm}' ({Count} items)", 
                searchTerm, cachedResults.Count);
            return cachedResults;
        }

        try
        {
            var results = await GetAsync<List<ClientLookupDto>>(
                $"/api/clients/search?query={Uri.EscapeDataString(searchTerm)}") ?? [];

            // ✅ Mettre en cache
            cacheService.Set(cacheKey, results, SearchCacheDuration);
            
            Logger.LogInformation("✅ Client search from API: '{SearchTerm}' ({Count} items)", 
                searchTerm, results.Count);
            
            return results;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error searching clients '{SearchTerm}'", searchTerm);
            return [];
        }
    }

    /// <summary>
    /// Récupère les adresses d'un client
    /// </summary>
    public async Task<List<ClientAddressDto>> GetClientAddressesAsync(int clientId)
    {
        try
        {
            var addresses = await GetAsync<List<ClientAddressDto>>($"/api/clients/{clientId}/addresses");
            return addresses ?? [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading addresses for client {ClientId}", clientId);
            return [];
        }
    }

    /// <summary>
    /// Récupère l'historique des livraisons d'un client avec cache
    /// </summary>
    public async Task<List<DeliveryDto>> GetClientDeliveriesAsync(int clientId)
    {
        var cacheKey = $"{CacheKeyDeliveriesPrefix}{clientId}";

        // ✅ Vérifier le cache
        var cachedDeliveries = cacheService.Get<List<DeliveryDto>>(cacheKey);
        if (cachedDeliveries != null)
        {
            Logger.LogDebug("✅ Deliveries for client {ClientId} found in cache ({Count} items)", 
                clientId, cachedDeliveries.Count);
            return cachedDeliveries;
        }

        try
        {
            var deliveries = await GetAsync<List<DeliveryDto>>($"/api/clients/{clientId}/deliveries");

            if (deliveries == null)
            {
                Logger.LogWarning("⚠️ No deliveries found for client {ClientId}", clientId);
                return [];
            }
            
            // ✅ Mettre en cache
            cacheService.Set(cacheKey, deliveries, CacheDuration);
            Logger.LogDebug("✅ Deliveries for client {ClientId} loaded from API and cached ({Count} items)", 
                clientId, deliveries.Count);

            return deliveries;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error loading deliveries for client {ClientId}", clientId);
            return [];
        }
    }

    #endregion

    #region UPDATE Operations

    /// <summary>
    /// Met à jour un client
    /// </summary>
    public async Task<ResponseResult> UpdateClientAsync(int id, UpdateClientDto dto)
    {
        try
        {
            Logger.LogInformation("📝 Updating client {Id}", id);

            var result = await PutAsync($"/api/clients/{id}", dto);

            if (result.Succeeded)
            {
                // ✅ Invalider le cache
                InvalidateClientCache(id);
                
                Logger.LogInformation("✅ Client {Id} updated successfully", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error updating client {Id}", id);
            return ResponseResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Ajoute une nouvelle adresse
    /// </summary>
    public async Task<ResponseResult<int>> AddAddressAsync(int clientId, CreateClientAddressDto dto)
    {
        try
        {
            Logger.LogInformation("📝 Adding address to client {ClientId}", clientId);

            var client = await CreateAuthorizedClientAsync();
            var response = await client.PostAsJsonAsync($"/api/clients/{clientId}/addresses", dto);

            if (response.IsSuccessStatusCode)
            {
                var addressId = await response.Content.ReadFromJsonAsync<int>();
                
                // ✅ Invalider le cache
                InvalidateClientCache(clientId);
                
                Logger.LogInformation("✅ Address added to client {ClientId}", clientId);
                return ResponseResult<int>.Success(addressId);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Logger.LogWarning("⚠️ Add address failed: {Error}", errorContent);
            
            return ResponseResult<int>.Failure(errorContent);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error adding address to client {ClientId}", clientId);
            return ResponseResult<int>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Met à jour une adresse existante
    /// </summary>
    public async Task<ResponseResult> UpdateAddressAsync(int clientId, int addressId, UpdateClientAddressDto dto)
    {
        try
        {
            Logger.LogInformation("📝 Updating address {AddressId} for client {ClientId}", addressId, clientId);

            var result = await PutAsync($"/api/clients/{clientId}/addresses/{addressId}", dto);

            if (result.Succeeded)
            {
                // ✅ Invalider le cache
                InvalidateClientCache(clientId);
                
                Logger.LogInformation("✅ Address {AddressId} updated", addressId);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error updating address {AddressId}", addressId);
            return ResponseResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Définit une adresse comme par défaut
    /// </summary>
    public async Task<ResponseResult> SetDefaultAddressAsync(int clientId, int addressId)
    {
        try
        {
            Logger.LogInformation("📝 Setting default address {AddressId} for client {ClientId}", addressId, clientId);

            var result = await PutAsync($"/api/clients/{clientId}/addresses/{addressId}/set-default");

            if (!result.Succeeded) return result;
            
            // ✅ Invalider le cache
            InvalidateClientCache(clientId);
                
            Logger.LogInformation("✅ Default address set to {AddressId}", addressId);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error setting default address {AddressId}", addressId);
            return ResponseResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Supprime une adresse
    /// </summary>
    public async Task<ResponseResult> DeleteAddressAsync(int clientId, int addressId)
    {
        try
        {
            Logger.LogInformation("🗑️ Deleting address {AddressId} from client {ClientId}", addressId, clientId);

            var result = await DeleteAsync($"/api/clients/{clientId}/addresses/{addressId}");

            if (result.Succeeded)
            {
                // ✅ Invalider le cache
                InvalidateClientCache(clientId);
                
                Logger.LogInformation("✅ Address {AddressId} deleted", addressId);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error deleting address {AddressId}", addressId);
            return ResponseResult.Failure(ex.Message);
        }
    }

    #endregion

    #region DELETE Operations

    /// <summary>
    /// Supprime un client (vérifie les livraisons côté backend)
    /// </summary>
    public async Task<ResponseResult> DeleteClientAsync(int id)
    {
        try
        {
            Logger.LogInformation("🗑️ Deleting client {Id}", id);

            var result = await DeleteAsync($"/api/clients/{id}");

            if (result.Succeeded)
            {
                // ✅ Invalider le cache
                InvalidateClientCache(id);
                
                Logger.LogInformation("✅ Client {Id} deleted successfully", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ Error deleting client {Id}", id);
            return ResponseResult.Failure(ex.Message);
        }
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Invalide tout le cache clients
    /// </summary>
    public void InvalidateCache()
    {
        cacheService.Remove(CacheKeyClientPrefix);
        cacheService.Remove(CacheKeySearchPrefix);
        Logger.LogInformation("🗑️ Clients cache invalidated");
    }

    /// <summary>
    /// Invalide le cache d'un client spécifique
    /// </summary>
    public void InvalidateClientCache(int id)
    {
        var cacheKey = $"{CacheKeyClientPrefix}{id}";
        var deliveriesCacheKey = $"{CacheKeyDeliveriesPrefix}{id}";
        
        cacheService.Remove(cacheKey);
        cacheService.Remove(deliveriesCacheKey);
        
        // Invalider aussi le cache de recherche
        cacheService.Remove(CacheKeySearchPrefix);
        
        Logger.LogInformation("🗑️ Client {Id} cache invalidated", id);
    }

    /// <summary>
    /// Force le rechargement depuis l'API
    /// </summary>
    public async Task RefreshAsync()
    {
        InvalidateCache();
        Logger.LogInformation("🔄 Clients cache refreshed");
        await Task.CompletedTask;
    }

    #endregion
}