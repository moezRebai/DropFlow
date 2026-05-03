using DropFlow.Shared.Common;
using DropFlow.Shared.Routes;

namespace DropFlow.Application.Interfaces;

public interface IRouteService
{
    Task<PagedResult<RouteViewDto>> GetAllAsync(RouteFilterDto filter);
    Task<ResponseResult<RouteDto>> GetByIdAsync(int id);
    Task<ResponseResult<int>> CreateAsync(CreateRouteDto dto);
    Task<ResponseResult> UpdateAsync(int id, UpdateRouteDto dto);
    Task<ResponseResult> DeleteAsync(int id);
    
    // Team Management
    Task<ResponseResult> AddTeamMemberAsync(int routeId, TeamMemberDto dto);
    Task<ResponseResult> RemoveTeamMemberAsync(int routeId, int driverId);
    
    // Delivery Management
    Task<ResponseResult> AddDeliveryAsync(int routeId, int deliveryId);
    Task<ResponseResult> RemoveDeliveryAsync(int routeId, int deliveryId);
    Task<ResponseResult> UpdateSequenceAsync(int routeId, List<UpdateDeliverySequenceDto> sequences);
    
    // Status Management
    Task<ResponseResult> ConfirmAsync(int id);
    Task<ResponseResult> StartAsync(int id);
    Task<ResponseResult> CompleteAsync(int id);
    Task<ResponseResult> CancelAsync(int id);
    
    // Metrics
    Task<ResponseResult<OptimizePathResponseDto>> RecalculateRouteMetricsAsync(
        OptimizePathRequestDto request);
    Task<ResponseResult<OptimizePathResponseDto>> OptimizeRouteAsync(OptimizePathRequestDto request);

    Task<ResponseResult<byte[]>> GenerateRouteSheetPdfAsync(int routeId);
}