using DropFlow.Shared.Common;
using DropFlow.Shared.Profil;
using DropFlow.Shared.UserManagement;
using DropFlow.WebApp.Interfaces;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services.Users;

public class UserManagementService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger<UserManagementService> logger)
    : BaseApiService(httpClientFactory,localStorage, logger), IUserManagementService
{
    public async Task<List<UserProfileDto>> GetUsersAsync(
        bool includeDeactivated = false, 
        bool includeDeleted = false)
    {
        var endpoint = $"/api/usermanagement/users?" +
                  $"includeDeactivated={includeDeactivated}&" +
                  $"includeDeleted={includeDeleted}";
        
        var users = await GetAsync<List<UserProfileDto>>(endpoint);
        return users ?? [];
    }
    public async Task<ResponseResult> InviteUserAsync(InviteUserDto request)
    {
        // POST avec body, mais sans response body attendue
        return await PostAsync($"/api/usermanagement/invite", request);
    }
    public async Task<ResponseResult> ActivateUserAsync(string userId)
    {
        return await PostAsync($"/api/usermanagement/{userId}/activate");
    }
    public async Task<ResponseResult> DeactivateUserAsync(string userId)
    {
        return await PostAsync($"/api/usermanagement/{userId}/deactivate");
    }
    public async Task<ResponseResult> ChangeUserRoleAsync(string userId, string newRole)
    {
        var request = new ChangeUserRoleDto(newRole);
        return await PutAsync($"/api/usermanagement/users/{userId}/role", request);
    }
    
    public async Task<ResponseResult> DeleteUserAsync(string userId)
    {
        return await DeleteAsync($"/api/usermanagement/users/{userId}");
    }

    public async Task<ResponseResult> RestoreUserAsync(string userId)
    {
        return await PostAsync($"/api/usermanagement/users/{userId}/restore");
    }
}