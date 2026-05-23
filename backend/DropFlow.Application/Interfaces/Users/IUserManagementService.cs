using DropFlow.Shared.Common;
using DropFlow.Shared.Profil;
using DropFlow.Shared.UserManagement;

namespace DropFlow.Application.Interfaces.Users;

public interface IUserManagementService
{
    Task<ResponseResult> InviteUserAsync(InviteUserDto dto, string invitedBy);
    Task<ResponseResult> DeactivateUserAsync(string userId, string deactivatedBy);
    Task<ResponseResult> ReactivateUserAsync(string userId, string reactivatedBy);
    Task<List<UserProfileDto>> GetTenantUsersAsync(int tenantId, 
        bool includeDeactivated = false, 
        bool includeDeleted = false);
    Task<ResponseResult> ChangeUserRoleAsync(string userId, string newRole);
    
    Task<ResponseResult> DeleteUserAsync(string userId);
    Task<ResponseResult> RestoreUserAsync(string userId);
}