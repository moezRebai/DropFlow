using DropFlow.Shared.Common;
using DropFlow.Shared.Deliveries;
using DropFlow.Shared.Routes;

namespace DropFlow.WebApp.Interfaces;

public interface IRouteService
{
    // READ
    Task<PagedResult<RouteViewDto>> GetRoutesAsync(RouteFilterDto filter);
    Task<RouteDto?> GetRouteByIdAsync(int id);
    Task<List<DeliveryDto>> GetUnassignedDeliveriesAsync(DateTime date);

    // CREATE
    Task<ResponseResult> CreateRouteAsync(CreateRouteDto dto);
    Task<ResponseResult<OptimizePathResponseDto>> OptimizeRouteAsync(OptimizePathRequestDto request);
    Task<ResponseResult<OptimizePathResponseDto>> RecalculatePathMetricsAsync(OptimizePathRequestDto request);
    Task<ResponseResult> AddTeamMemberAsync(int routeId, TeamMemberDto dto);
    Task<ResponseResult> AddDeliveryAsync(int routeId, int deliveryId);

    // UPDATE
    Task<ResponseResult> UpdateRouteAsync(int id, UpdateRouteDto dto);
    Task<ResponseResult> UpdateSequenceAsync(int id, List<UpdateDeliverySequenceDto> sequences);

    // DELETE
    Task<ResponseResult> DeleteRouteAsync(int id);
    Task<ResponseResult> RemoveTeamMemberAsync(int routeId, int driverId);
    Task<ResponseResult> RemoveDeliveryAsync(int routeId, int deliveryId);

    // WORKFLOW
    Task<ResponseResult> ConfirmAsync(int id);
    Task<ResponseResult> StartAsync(int id);
    Task<ResponseResult> CompleteAsync(int id);
    Task<ResponseResult> CancelAsync(int id);
    /// <summary>
    /// Retourne l'URL de téléchargement direct de la feuille de route
    /// </summary>
    Task<(bool Success, byte[]? PdfBytes, string? ErrorMessage)> DownloadRouteSheetAsync(int routeId);
    void InvalidateCache();
    void InvalidateRouteCache(int id);
}