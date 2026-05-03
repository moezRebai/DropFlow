using DropFlow.Shared.Common;
using DropFlow.Shared.Drivers;

namespace DropFlow.Application.Interfaces;

public interface IDriverService
{
    Task<PagedResult<DriverDto>> GetAllAsync(DriverFilterDto filter);
    Task<ResponseResult<DriverDto>> GetByIdAsync(int id);
    Task<ResponseResult<DriverDto>> GetByUserIdAsync(string userId);
    Task<ResponseResult<int>> CreateAsync(CreateDriverDto dto);
    Task<ResponseResult> UpdateAsync(int id, UpdateDriverDto dto);
    Task<ResponseResult> DeleteAsync(int id);
    Task<List<DriverDto>> GetActiveDriversAsync();
}