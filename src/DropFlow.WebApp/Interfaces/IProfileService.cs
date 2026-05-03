using DropFlow.Shared.Common;
using DropFlow.Shared.Profil;
using DropFlow.WebApp.Models.Profile;

namespace DropFlow.WebApp.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto?> GetProfileAsync();
    Task<ResponseResult> UpdateProfileAsync(UpdateProfileRequest request);
    Task<ResponseResult> ChangePasswordAsync(ChangePasswordRequest request);
    Task<UserPreferencesDto?> GetPreferencesAsync();
    Task<ResponseResult> UpdatePreferencesAsync(UserPreferencesDto request);
    void InvalidateCache();
}