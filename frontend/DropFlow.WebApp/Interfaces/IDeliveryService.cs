using DropFlow.Shared.Common;
using DropFlow.Shared.Deliveries;
using DropFlow.Shared.Enums;

namespace DropFlow.WebApp.Interfaces;

/// <summary>
/// Interface du service de gestion des livraisons côté frontend
/// </summary>
public interface IDeliveryService
{
    // ═══ READ ═══
    Task<ResponseResult<DeliveryDto>> GetDeliveryByIdAsync(int id);
    Task<PagedResult<DeliveryViewDto>> GetDeliveriesAsync(DeliveryFilterDto filter);
    Task<DeliveryStatsDto> GetStatsAsync();
    
    // ═══ CREATE & UPDATE ═══
    Task<ResponseResult> CreateDeliveryAsync(CreateDeliveryDto dto);
    Task<ResponseResult> UpdateDeliveryAsync(int id, UpdateDeliveryDto dto);
    Task<ResponseResult> DuplicateDeliveryAsync(int id);
    
    // ═══ DELETE ═══
    Task<ResponseResult> DeleteDeliveryAsync(int id);
    Task<ResponseResult> BulkDeleteAsync(List<int> ids);
    
    // ═══ STATUS ═══
    Task<ResponseResult> UpdateStatusAsync(int id, DeliveryStatus status);
    Task<ResponseResult> BulkUpdateStatusAsync(List<int> ids, DeliveryStatus status);
    
    // ═══ ✨ NOUVELLES MÉTHODES - ROUTES ═══
    
    /// <summary>
    /// Récupère les livraisons disponibles pour ajout à une tournée
    /// Exclut automatiquement les livraisons verrouillées dans des tournées actives
    /// </summary>
    /// <param name="date">Date de la tournée</param>
    /// <param name="currentRouteId">ID de la tournée courante (pour mode édition)</param>
    /// <returns>Liste des livraisons disponibles avec indication si déjà dans route Draft</returns>
    Task<ResponseResult<List<DeliveryDto>>> GetAvailableDeliveriesForRouteAsync(
        DateTime date,
        int? currentRouteId = null);

    Task<ResponseResult<DeliveryDto>> GeocodeDeliveryAsync(int id);
}