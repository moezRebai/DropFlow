using DropFlow.Shared.Common;
using DropFlow.Shared.Drivers;

namespace DropFlow.WebApp.Interfaces;

public interface IDriverService
{
    // READ Operations
    Task<PagedResult<DriverDto>> GetPagedAsync(DriverFilterDto filter);
    Task<List<DriverDto>> GetAllAsync(bool forceRefresh = false);
    Task<DriverDto?> GetByIdAsync(int id);
    Task<DriverDto?> GetByUserIdAsync(string userId);
    Task<List<DriverDto>> GetActiveDriversAsync();
    
    // Availability
    Task<DriverAvailabilityDto> CheckAvailabilityAsync(int driverId, DateTime date);
    Task<List<DriverAvailabilityDto>> CheckMultipleAvailabilityAsync(List<int> driverIds, DateTime date);
    Task<List<DriverDto>> GetAvailableDriversAsync(DateTime date);
    
    // WRITE Operations
    Task<ResponseResult> CreateAsync(CreateDriverDto dto);
    Task<ResponseResult> UpdateAsync(int id, UpdateDriverDto dto);
}