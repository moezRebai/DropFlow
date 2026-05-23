using DropFlow.Shared.Common;
using DropFlow.Shared.Stores;

namespace DropFlow.Application.Interfaces;

public interface IStoreService
{
    // CRUD
    Task<ResponseResult<int>> CreateStoreAsync(CreateStoreDto dto);
    Task<ResponseResult> UpdateStoreAsync(int id, UpdateStoreDto dto);
    Task<ResponseResult> DeleteStoreAsync(int id);
    Task<ResponseResult<StoreDto>> GetStoreByIdAsync(int id);
    Task<PagedResult<StoreDto>> GetStoresAsync(StoreFilterDto filter);
    Task<List<StoreDto>> GetAllStoresAsync();
    Task<List<StoreLookupDto>> GetStoresLookupAsync();
    Task<ResponseResult> ActivateStoreAsync(int id);
    Task<ResponseResult> DeactivateStoreAsync(int id);
}