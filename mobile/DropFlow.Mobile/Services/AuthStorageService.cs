using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using DropFlow.Mobile.Models;

namespace DropFlow.Mobile.Services;

public class AuthStorageService
{
    private const string TokenKey = "auth_token";
    private const string UserInfoKey = "user_info";
    private const string RefreshTokenKey = "refresh_token";
    private const string RememberMeKey = "remember_me";

    public async Task SaveAsync(string token, UserInfo user, string? refreshToken = null, bool rememberMe = false)
    {
        await SecureStorage.Default.SetAsync(TokenKey, token);
        await SecureStorage.Default.SetAsync(UserInfoKey, JsonSerializer.Serialize(user));
        await SecureStorage.Default.SetAsync(RememberMeKey, rememberMe.ToString());

        if (rememberMe && !string.IsNullOrEmpty(refreshToken))
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
        else
            SecureStorage.Default.Remove(RefreshTokenKey);
    }

    public async Task SaveTokenAsync(string token)
        => await SecureStorage.Default.SetAsync(TokenKey, token);

    public async Task<string?> GetTokenAsync()
        => await SecureStorage.Default.GetAsync(TokenKey);

    public async Task<string?> GetRefreshTokenAsync()
        => await SecureStorage.Default.GetAsync(RefreshTokenKey);

    public async Task<UserInfo?> GetUserInfoAsync()
    {
        var json = await SecureStorage.Default.GetAsync(UserInfoKey);
        return json is null ? null : JsonSerializer.Deserialize<UserInfo>(json);
    }

    public async Task<bool> IsRememberMeAsync()
    {
        var val = await SecureStorage.Default.GetAsync(RememberMeKey);
        return bool.TryParse(val, out var result) && result;
    }

    public async Task<bool> HasValidTokenAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token)) return false;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo > DateTime.UtcNow.AddMinutes(1);
        }
        catch
        {
            return false;
        }
    }

    public void Clear()
    {
        SecureStorage.Default.Remove(TokenKey);
        SecureStorage.Default.Remove(UserInfoKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(RememberMeKey);
    }
}
