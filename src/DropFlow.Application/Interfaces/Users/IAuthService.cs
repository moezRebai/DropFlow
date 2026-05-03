using DropFlow.Shared.Auth;
using DropFlow.Shared.Common;

namespace DropFlow.Application.Interfaces.Users;

public interface IAuthService
{
    Task<AuthResult> RegisterTenantAsync(RegisterTenantDto dto);
    Task<AuthResult> LoginAsync(LoginDto dto);
    Task<AuthResult> AcceptInvitationAsync(AcceptInvitationDto dto);
    Task<ResponseResult> ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<ResponseResult> ResetPasswordAsync(ResetPasswordDto dto);
    Task<List<UserTenantInfoDto>> GetUserTenantsAsync(string email);
}