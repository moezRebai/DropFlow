using DropFlow.Shared.Common;
using DropFlow.Shared.Profil;

namespace DropFlow.Application.Interfaces.Users;

public interface IProfileService
{
    // Profile
    Task<UserProfileDto?> GetCurrentUserProfileAsync();
    Task<ResponseResult> UpdateProfileAsync(UpdateProfileDto dto);
    
    // Password
    Task<ResponseResult> ChangePasswordAsync(ChangePasswordDto dto);
    
    // Preferences
    Task<UserPreferencesDto> GetPreferencesAsync();
    Task<ResponseResult> UpdatePreferencesAsync(UserPreferencesDto dto);
}