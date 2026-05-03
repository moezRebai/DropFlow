using DropFlow.Shared.Clients;
using DropFlow.Shared.Common;
using DropFlow.Shared.Deliveries;

namespace DropFlow.Application.Interfaces;

public interface IClientService
{
    Task<ResponseResult<int>> CreateClientAsync(CreateClientDto dto);
    Task<ResponseResult> UpdateClientAsync(int id, UpdateClientDto dto);
    Task<ResponseResult> DeleteClientAsync(int id);
    Task<ResponseResult<ClientDto>> GetClientByIdAsync(int id);
    Task<List<ClientLookupDto>> SearchClientsAsync(string searchTerm);
    Task<List<ClientAddressDto>> GetClientAddressesAsync(int clientId);
    Task<ResponseResult<int>> AddAddressAsync(int clientId, CreateClientAddressDto dto);
    Task<ResponseResult> SetDefaultAddressAsync(int clientId, int addressId);
    Task<ResponseResult> DeleteAddressAsync(int clientId, int addressId);
    Task<PagedResult<ClientDto>> GetClientsAsync(ClientFilterDto filter);
    Task<List<DeliveryDto>> GetClientDeliveriesAsync(int clientId);
    Task<ResponseResult> UpdateAddressAsync(int clientId, int addressId, UpdateClientAddressDto dto);
}