using DropFlow.Shared.Auth;
using DropFlow.WebApp.Models.Auth;

namespace DropFlow.WebApp.Interfaces;
public interface IAuthService
{
    Task<List<UserTenantInfoDto>> GetUserTenantsAsync(string email);
    Task<LoginResponse> LoginAsync(LoginRequest loginRequest);
    Task<LoginResponse> RegisterAsync(RegisterRequest registerModel);
    Task LogoutAsync();
    Task<LoginResponse> RefreshTokenAsync();
    Task<TokenInfo?> GetTokenInfoAsync();
    Task<PasswordResetResponse> ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<PasswordResetResponse> ResetPasswordAsync(ResetPasswordDto dto);
    Task<AuthResult> AcceptInvitationAsync(AcceptInvitationDto dto);
}