using DropFlow.Shared.Common;
using DropFlow.Shared.Profil;
using DropFlow.Shared.UserManagement;

namespace DropFlow.WebApp.Interfaces;

public interface IUserManagementService
{
    Task<List<UserProfileDto>> GetUsersAsync( bool includeDeactivated = false, 
        bool includeDeleted = false);
    Task<ResponseResult> InviteUserAsync(InviteUserDto request);
    Task<ResponseResult> ActivateUserAsync(string userId);
    Task<ResponseResult> DeactivateUserAsync(string userId);
    Task<ResponseResult> ChangeUserRoleAsync(string userId, string newRole);
    Task<ResponseResult> DeleteUserAsync(string userId);
    Task<ResponseResult> RestoreUserAsync(string userId);
}