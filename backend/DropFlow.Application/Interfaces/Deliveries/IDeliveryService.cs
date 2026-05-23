using DropFlow.Shared.Enums;
using DropFlow.Shared.Common;
using DropFlow.Shared.Deliveries;
using DropFlow.Shared.Routes;

namespace DropFlow.Application.Interfaces.Deliveries;

public interface IDeliveryService
{
    Task<ResponseResult<int>> CreateDeliveryAsync(CreateDeliveryDto dto);
    Task<ResponseResult> UpdateDeliveryAsync(int id, UpdateDeliveryDto dto);
    Task<ResponseResult> DeleteDeliveryAsync(int id);
    Task<ResponseResult<DeliveryDto>> GetDeliveryByIdAsync(int id);
    Task<PagedResult<DeliveryViewDto>> GetDeliveriesAsync(DeliveryFilterDto filter);
    Task<ResponseResult> UpdateStatusAsync(int id, DeliveryStatus status);
    Task<ResponseResult> BulkUpdateStatusAsync(List<int> ids, DeliveryStatus status);
    Task<ResponseResult> BulkDeleteAsync(List<int> ids);
    Task<ResponseResult> DuplicateDeliveryAsync(int id);
    Task<DeliveryStatsDto> GetStatsAsync();
    Task<ResponseResult<List<DeliveryDto>>> GetUnassignedDeliveriesAsync(DateTime date);
    
    // --- ? NOUVELLES MÉTHODES - VALIDATION ROUTE ---
    
    /// <summary>
    /// Vérifie si une livraison peut être ajoutée à une tournée
    /// Règles :
    /// - ? OK si pas encore dans une tournée
    /// - ? OK si dans tournées Draft uniquement
    /// - ? OK si dans tournées Cancelled uniquement
    /// - ? KO si dans une tournée Confirmed/InProgress/Completed
    /// </summary>
    /// <param name="deliveryId">ID de la livraison</param>
    /// <param name="excludeRouteId">ID de la tournée à exclure (pour édition)</param>
    /// <returns>True si disponible, False sinon</returns>
    Task<bool> IsDeliveryAvailableForRouteAsync(int deliveryId, int? excludeRouteId = null);
    
    /// <summary>
    /// Récupère la tournée active dans laquelle la livraison est verrouillée
    /// </summary>
    /// <param name="deliveryId">ID de la livraison</param>
    /// <returns>RouteDto si verrouillée, null si disponible</returns>
    Task<RouteDto?> GetActiveRouteForDeliveryAsync(int deliveryId);
    
    /// <summary>
    /// Récupère les livraisons disponibles pour ajout à une tournée
    /// Filtre les livraisons déjà dans des tournées actives (Confirmed/InProgress/Completed)
    /// </summary>
    /// <param name="date">Date de la tournée</param>
    /// <param name="currentRouteId">ID de la tournée courante (pour édition)</param>
    /// <returns>Liste des livraisons disponibles</returns>
    Task<ResponseResult<List<DeliveryDto>>> GetAvailableDeliveriesForRouteAsync(
        DateTime date,
        int? currentRouteId = null);

    Task<ResponseResult<DeliveryDto>> GeocodeDeliveryAsync(int id);
}