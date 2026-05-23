using DropFlow.Shared.Common;
using DropFlow.Shared.Drivers;

namespace DropFlow.Application.Interfaces.Drivers;

/// <summary>
/// Service dédié à l'application mobile livreur
/// Tous les endpoints filtrent par le Driver lié au User connecté
/// </summary>
public interface IDriverAppService
{
    /// <summary>
    /// Récupère la tournée du jour du livreur connecté
    /// Cherche via JWT UserId → Driver → RouteTeam → Route (date = today, status = Confirmed ou InProgress)
    /// </summary>
    Task<DriverTodayResponse> GetTodayRouteAsync();
    
    /// <summary>
    /// Récupère le détail d'une livraison (vue livreur)
    /// Vérifie que la livraison appartient bien à une route assignée au livreur
    /// </summary>
    Task<ResponseResult<DriverDeliveryDetailDto>> GetDeliveryDetailAsync(int deliveryId);
    
    /// <summary>
    /// Valide une livraison : signature + photo + commentaire
    /// Passe le statut à Delivered, sauvegarde les fichiers sur disque
    /// </summary>
    Task<ResponseResult<bool>> ValidateDeliveryAsync(int deliveryId, ValidateDeliveryDto dto);
    
    /// <summary>
    /// Démarre la tournée du livreur
    /// Passe la route en InProgress et toutes les livraisons Confirmed en InProgress
    /// </summary>
    Task<ResponseResult<bool>> StartRouteAsync(int routeId);
    
    /// <summary>
    /// Termine la tournée du livreur
    /// Passe la route en Completed
    /// </summary>
    Task<ResponseResult<bool>> CompleteRouteAsync(int routeId);

    Task<DriverDashboardResponse> GetDashboardAsync();
    Task<List<DriverRouteSummaryDto>> GetUpcomingRoutesAsync();
    Task<DriverTodayResponse> GetRouteDetailAsync(int routeId);
    Task<DriverHistoryResponse> GetDeliveryHistoryAsync(int page, int pageSize);
}
