using DropFlow.Shared.Enums;
using DropFlow.Shared.Common;
using DropFlow.Shared.Deliveries;
using DropFlow.WebApp.Interfaces;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services;

public class DeliveryService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger<DeliveryService> logger,
    IDeliveryEventBus eventBus)
    : BaseApiService(httpClientFactory, localStorage, logger), IDeliveryService
{
    
    // ----------------------------------------------------------------
    // LISTE & FILTRES
    // ----------------------------------------------------------------

    /// <summary>
    /// Récupère la liste paginée des livraisons avec filtres
    /// </summary>
    public async Task<PagedResult<DeliveryViewDto>> GetDeliveriesAsync(DeliveryFilterDto filters)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={filters.Page}",
                $"pageSize={filters.PageSize}"
            };

            if (filters.StoreId.HasValue)
                queryParams.Add($"storeId={filters.StoreId.Value}");

            if (filters.Statuses != null && filters.Statuses.Any())
            {
                // Ajouter chaque statut comme paramètre séparé pour la query string
                queryParams.AddRange(filters.Statuses.Select(status => $"statuses={status}"));
            }

            if (filters.DateFrom.HasValue)
                queryParams.Add($"dateFrom={filters.DateFrom.Value:yyyy-MM-dd}");

            if (filters.DateTo.HasValue)
                queryParams.Add($"dateTo={filters.DateTo.Value:yyyy-MM-dd}");

            if (!string.IsNullOrWhiteSpace(filters.GlobalSearch))
                queryParams.Add($"globalSearch={Uri.EscapeDataString(filters.GlobalSearch)}");

            // ?? LE POINT QUI MANQUAIT
            if (!string.IsNullOrWhiteSpace(filters.SortBy))
            {
                queryParams.Add($"sortBy={filters.SortBy}");
                queryParams.Add($"sortDescending={filters.SortDescending}");
            }

            var endpoint = $"/api/deliveries?{string.Join("&", queryParams)}";

            Logger.LogInformation("Fetching deliveries: {Endpoint}", endpoint);

            return await GetAsync<PagedResult<DeliveryViewDto>>(endpoint) ?? new PagedResult<DeliveryViewDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching deliveries");
            return new PagedResult<DeliveryViewDto>();
        }
    }

    public async Task<ResponseResult<DeliveryDto>> GetDeliveryByIdAsync(int id)
    {
        Logger.LogInformation("Fetching delivery details: {Id}", id);
        var responseResult = await GetAsync<ResponseResult<DeliveryDto>>($"/api/deliveries/{id}");
        return responseResult ?? null;
    }
    
    // ----------------------------------------------------------------
    // CRÉATION & MODIFICATION
    // ----------------------------------------------------------------

    /// <summary>
    /// Crée une nouvelle livraison
    /// </summary>
    public async Task<ResponseResult> CreateDeliveryAsync(CreateDeliveryDto request)
    {
        Logger.LogInformation("Creating new delivery for store {StoreId}", request.StoreId);
        var result = await PostAsync("/api/Deliveries", request);
        
        if (result.Succeeded)
        {
            // ? Déclencher l'événement de création
            Logger.LogInformation("?? Delivery created successfully, triggering OnDeliveryCreated event");
            
            // Note: L'API ne retourne pas toujours l'ID dans ResponseResult
            // Si besoin de l'ID, il faudrait modifier l'API pour retourner ResponseResult<int>
            eventBus.TriggerDeliveryCreated(0); // ID pas disponible dans ResponseResult standard
        }
        else
        {
            Logger.LogWarning("?? Delivery creation failed");
        }
        
        return result;
    }
    
    /// <summary>
    /// Met à jour une livraison existante
    /// </summary>
    public async Task<ResponseResult> UpdateDeliveryAsync(int id, UpdateDeliveryDto request)
    {
        Logger.LogInformation("Updating delivery {Id}", id);
        var result = await PutAsync($"/api/deliveries/{id}", request);
        
        if (result.Succeeded)
        {
            Logger.LogInformation("?? Delivery {Id} updated successfully, triggering OnDeliveryUpdated event", id);
            eventBus.TriggerDeliveryUpdated(id);
        }
        else
        {
            Logger.LogWarning("?? Delivery {Id} update failed", id);
        }
        
        return result;
    }

    // ----------------------------------------------------------------
    // SUPPRESSION & DUPLICATION
    // ----------------------------------------------------------------

    /// <summary>
    /// Supprime une livraison
    /// </summary>
    public async Task<ResponseResult> DeleteDeliveryAsync(int id)
    {
        Logger.LogInformation("Deleting delivery {Id}", id);
        var result = await DeleteAsync($"/api/deliveries/{id}");
        
        if (result.Succeeded)
        {
            // ? Déclencher l'événement de suppression
            Logger.LogInformation("?? Delivery {Id} deleted successfully, triggering OnDeliveryDeleted event", id);
            eventBus.TriggerDeliveryDeleted(id);
        }
        else
        {
            Logger.LogWarning("?? Delivery {Id} deletion failed", id);
        }
        
        return result;
    }

    public Task<ResponseResult> UpdateStatusAsync(int id, DeliveryStatus status)
    {
        throw new NotImplementedException();
    }
    public Task<ResponseResult> BulkUpdateStatusAsync(List<int> deliveryIds, DeliveryStatus status)
    {
        throw new NotImplementedException();
    }

    public Task<ResponseResult> BulkDeleteAsync(List<int> deliveryIds)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Duplique une livraison existante
    /// </summary>
    public async Task<ResponseResult> DuplicateDeliveryAsync(int id)
    {
        Logger.LogInformation("Duplicating delivery {Id}", id);
        return await PostAsync($"/api/deliveries/{id}/duplicate");
    }

    // ----------------------------------------------------------------
    // CHANGEMENT DE STATUT
    // ----------------------------------------------------------------

    /// <summary>
    /// Change le statut d'une livraison
    /// </summary>
    public async Task<ResponseResult> ChangeStatusAsync(int id, int newStatus)
    {
        Logger.LogInformation("Changing delivery {Id} status to {Status}", id, newStatus);
        var request = new { Status = newStatus };
        return await PostAsync($"/api/deliveries/{id}/status", request);
    }

    /// <summary>
    /// Change le statut de plusieurs livraisons en une fois
    /// </summary>
    public async Task<ResponseResult> ChangeStatusBatchAsync(List<int> ids, int newStatus)
    {
        Logger.LogInformation("Batch changing status for {Count} deliveries to {Status}", 
            ids.Count, newStatus);
            
        var request = new 
        { 
            DeliveryIds = ids, 
            Status = newStatus 
        };
        
        return await PostAsync("/api/deliveries/batch/status", request);
    }

    // ----------------------------------------------------------------
    // STATISTIQUES
    // ----------------------------------------------------------------

    /// <summary>
    /// Récupère les statistiques pour le dashboard
    /// </summary>
    public async Task<DeliveryStatsDto> GetStatsAsync()
    {
        Logger.LogInformation("Fetching delivery statistics");
        return await GetAsync<DeliveryStatsDto>("/api/deliveries/stats") ?? new DeliveryStatsDto();
    }
    
    /// <summary>
    /// Récupère les livraisons disponibles pour ajout à une tournée
    /// Exclut automatiquement les livraisons verrouillées dans des tournées actives
    /// </summary>
    public async Task<ResponseResult<DeliveryDto>> GeocodeDeliveryAsync(int id)
    {
        Logger.LogInformation("Geocoding delivery {Id}", id);
        var result = await PostAsync<object, ResponseResult<DeliveryDto>>(
            $"/api/deliveries/{id}/geocode", new { });
        return result ?? ResponseResult<DeliveryDto>.Failure("Réponse invalide du serveur");
    }

    public async Task<ResponseResult<List<DeliveryDto>>> GetAvailableDeliveriesForRouteAsync(
        DateTime date, 
        int? currentRouteId = null)
    {
        try
        {
            var queryString = $"?date={date:yyyy-MM-dd}";
        
            if (currentRouteId.HasValue)
            {
                queryString += $"&currentRouteId={currentRouteId.Value}";
            }
        
            Logger.LogInformation("?? Loading available deliveries for route (Date: {Date}, CurrentRouteId: {RouteId})",
                date.ToString("yyyy-MM-dd"), currentRouteId);
        
            var result = await GetAsync<ResponseResult<List<DeliveryDto>>>(
                $"/api/deliveries/available-for-route{queryString}");
        
            if (result is { Succeeded: true, Data: not null })
            {
                Logger.LogInformation("? Loaded {Count} available deliveries", result.Data.Count);
                return result;
            }
        
            Logger.LogWarning("?? No deliveries found or API error");
            return ResponseResult<List<DeliveryDto>>.Success(new List<DeliveryDto>());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "? Error loading available deliveries for route");
            return ResponseResult<List<DeliveryDto>>.Failure("Erreur lors du chargement des livraisons disponibles");
        }
    }
}