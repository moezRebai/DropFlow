using DropFlow.Shared.Common;
using DropFlow.Shared.Vehicles;

namespace DropFlow.WebApp.Interfaces;

public interface IVehicleService
{
    // READ Operations
    Task<List<VehicleDto>> GetAllVehiclesAsync(bool forceRefresh = false);
    Task<PagedResult<VehicleDto>> GetVehiclesAsync(VehicleFilterDto filter);
    Task<VehicleDto?> GetVehicleByIdAsync(int id);
    
    // Availability
    Task<bool> IsAvailableAsync(int vehicleId, DateTime date);
    Task<List<VehicleDto>> GetAvailableVehiclesAsync(DateTime date);
    
    // WRITE Operations
    Task<ResponseResult> CreateVehicleAsync(CreateVehicleDto dto);
    Task<ResponseResult> UpdateVehicleAsync(int id, UpdateVehicleDto dto);
    Task<ResponseResult> DeleteVehicleAsync(int id);
    
    // Cache Management
    void InvalidateCache();
    void InvalidateVehicleCache(int id);
    Task<List<VehicleDto>> RefreshAsync();
}