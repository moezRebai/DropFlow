using DropFlow.Shared.Common;
using DropFlow.Shared.Vehicles;

namespace DropFlow.Application.Interfaces;

public interface IVehicleService
{
    Task<PagedResult<VehicleDto>> GetAllAsync(VehicleFilterDto filter);
    Task<ResponseResult<VehicleDto>> GetByIdAsync(int id);
    Task<ResponseResult<int>> CreateAsync(CreateVehicleDto dto);
    Task<ResponseResult> UpdateAsync(int id, UpdateVehicleDto dto);
    Task<ResponseResult> DeleteAsync(int id);
    Task<ResponseResult<bool>> IsAvailableAsync(int vehicleId, DateTime date);
    Task<List<VehicleDto>> GetAvailableVehiclesAsync(DateTime date);
}