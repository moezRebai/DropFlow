using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using DropFlow.Shared.Auth;
using DropFlow.WebApp.Interfaces;
using DropFlow.WebApp.Models.Auth;
using DropFlow.WebApp.Providers;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services.Users;

public class AuthService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    JwtAuthenticationStateProvider authStateProvider)
    : IAuthService
{
    private const string TokenKey = "auth_token";
    private const string RefreshTokenKey = "refresh_token";

    // -------------------------
    // LOGIN
    // -------------------------
    public async Task<List<UserTenantInfoDto>> GetUserTenantsAsync(string email)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("DropFlowAPI");
            var response = await httpClient.GetAsync($"/api/auth/tenants?email={Uri.EscapeDataString(email)}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<UserTenantInfoDto>>() ?? [];
            }

            return [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("DropFlowAPI");
            var response = await httpClient.PostAsJsonAsync("api/auth/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (loginResponse?.Success == true && !string.IsNullOrEmpty(loginResponse.Token))
                {
                    await StoreTokensAsync(loginResponse.Token, null);
                    await authStateProvider.NotifyUserAuthentication(loginResponse.Token);
                    return loginResponse;
                }

                return loginResponse ?? Failed("Erreur de connexion.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return Failed("Email ou mot de passe incorrect.");
            }

            return Failed("Une erreur s'est produite lors de la connexion.");
        }
        catch (Exception ex)
        {
            return Failed($"Erreur de connexion: {ex.Message}");
        }
    }

    // -------------------------
    // REGISTER
    // -------------------------
    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("DropFlowAPI");
            var response = await httpClient.PostAsJsonAsync("api/auth/register", request);

            if (response.IsSuccessStatusCode)
            {
                var registerResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

                if (registerResponse?.Success == true && !string.IsNullOrEmpty(registerResponse.Token))
                {
                    await StoreTokensAsync(registerResponse.Token, null);
                    await authStateProvider.NotifyUserAuthentication(registerResponse.Token);
                    return registerResponse;
                }
            }

            return Failed("Une erreur s'est produite lors de l'inscription.");
        }
        catch (Exception ex)
        {
            return Failed($"Erreur d'inscription: {ex.Message}");
        }
    }

    // -------------------------
    // LOGOUT
    // -------------------------
    public async Task LogoutAsync()
    {
        try
        {
            await localStorage.DeleteAsync(TokenKey);
            await localStorage.DeleteAsync(RefreshTokenKey);
        }
        catch
        {
            // ignored
        }

        await authStateProvider.NotifyUserLogout();
    }

    // -------------------------
    // PASSWORD RESET
    // -------------------------
    public async Task<PasswordResetResponse> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("DropFlowAPI");
            var response = await httpClient.PostAsJsonAsync("api/auth/forgot-password", dto);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PasswordResetResponse>()
                       ?? Success("Un email de réinitialisation a été envoyé.");
            }

            return FailedReset(response);
        }
        catch (Exception ex)
        {
            return FailedReset(ex.Message);
        }
    }

    public async Task<PasswordResetResponse> ResetPasswordAsync(ResetPasswordDto dto)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("DropFlowAPI");
            var response = await httpClient.PostAsJsonAsync("api/auth/reset-password", dto);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PasswordResetResponse>()
                       ?? Success("Votre mot de passe a été réinitialisé avec succès.");
            }

            return FailedReset(response);
        }
        catch (Exception ex)
        {
            return FailedReset(ex.Message);
        }
    }

    // -------------------------
    // INVITATION
    // -------------------------
    public async Task<AuthResult> AcceptInvitationAsync(AcceptInvitationDto dto)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("DropFlowAPI");
            var response = await httpClient.PostAsJsonAsync("api/auth/accept-invitation", dto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResult>();

                if (result is { Success: true, Token: not null })
                {
                    await StoreTokensAsync(result.Token, null);
                    await authStateProvider.NotifyUserAuthentication(result.Token);
                }

                return result ?? new AuthResult(false);
            }

            return new AuthResult(false)
            {
                Message = ReadBackendError(response)
            };
        }
        catch (Exception ex)
        {
            return new AuthResult(false)
            {
                Message = ex.Message
            };
        }
    }

    // -------------------------
    // TOKEN HELPERS
    // -------------------------
    public async Task<TokenInfo?> GetTokenInfoAsync()
    {
        var token = await GetTokenAsync();
        return string.IsNullOrEmpty(token) ? null : ParseToken(token);
    }

    private async Task<string?> GetTokenAsync()
    {
        var result = await localStorage.GetAsync<string>(TokenKey);
        return result.Success ? result.Value : null;
    }

    private async Task StoreTokensAsync(string token, string? refreshToken)
    {
        try
        {
            await localStorage.DeleteAsync(TokenKey);
            await localStorage.DeleteAsync(RefreshTokenKey);

            await localStorage.SetAsync(TokenKey, token);

            if (!string.IsNullOrEmpty(refreshToken))
                await localStorage.SetAsync(RefreshTokenKey, refreshToken);
        }
        catch
        {
            // ignored
        }
    }

    private static TokenInfo? ParseToken(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            return new TokenInfo
            {
                UserId = jwt.Claims.FirstOrDefault(c => c.Type is "sub" or ClaimTypes.NameIdentifier)?.Value,
                UserName = jwt.Claims.FirstOrDefault(c => c.Type is "name" or ClaimTypes.Name)?.Value,
                Email = jwt.Claims.FirstOrDefault(c => c.Type is "email" or ClaimTypes.Email)?.Value,
                Roles = jwt.Claims
                    .Where(c => c.Type is "role" or ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList(),
                ExpiresAt = jwt.ValidTo
            };
        }
        catch
        {
            return null;
        }
    }

    // -------------------------
    // SMALL HELPERS
    // -------------------------
    private static LoginResponse Failed(string message) =>
        new() { Success = false, Message = message };

    private static PasswordResetResponse Success(string message) =>
        new() { Success = true, Message = message };

    private static PasswordResetResponse FailedReset(string message) =>
        new() { Success = false, Message = message };

    private static PasswordResetResponse FailedReset(HttpResponseMessage response)
    {
        try
        {
            var json = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
            return FailedReset(json.RootElement.GetProperty("message").GetString() ?? "Erreur.");
        }
        catch
        {
            return FailedReset("Une erreur s'est produite.");
        }
    }

    private static string ReadBackendError(HttpResponseMessage response)
    {
        try
        {
            var json = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
            return json.RootElement.GetProperty("message").GetString() ?? "Erreur.";
        }
        catch
        {
            return "Une erreur s'est produite.";
        }
    }
}