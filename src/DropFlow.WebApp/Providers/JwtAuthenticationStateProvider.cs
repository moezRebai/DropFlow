using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Providers;

public class JwtAuthenticationStateProvider(
    ProtectedLocalStorage localStorage)
    : AuthenticationStateProvider
{
    private const string TokenKey = "auth_token";

    private readonly ClaimsPrincipal _anonymous =
        new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
                return new AuthenticationState(_anonymous);

            if (IsTokenExpired(token))
                return new AuthenticationState(_anonymous);

            var claims = ParseClaimsFromToken(token);
            if (claims == null)
                return new AuthenticationState(_anonymous);

            var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    // -------------------------
    // TOKEN ACCESS
    // -------------------------
    private async Task<string?> GetTokenAsync()
    {
        var result = await localStorage.GetAsync<string>(TokenKey);
        return result.Success ? result.Value : null;
    }

    // -------------------------
    // CLAIMS
    // -------------------------
    private static IEnumerable<Claim>? ParseClaimsFromToken(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            return jwt.Claims.Select(claim =>
            {
                var type = claim.Type switch
                {
                    "sub"   => ClaimTypes.NameIdentifier,
                    "name"  => ClaimTypes.Name,
                    "email" => ClaimTypes.Email,
                    "role"  => ClaimTypes.Role,
                    _       => claim.Type
                };

                return new Claim(type, claim.Value);
            }).ToList();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsTokenExpired(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwt.ValidTo <= DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    // -------------------------
    // NOTIFICATIONS
    // -------------------------
    public Task NotifyUserAuthentication(string token)
    {
        var claims = ParseClaimsFromToken(token);
        if (claims == null)
            return Task.CompletedTask;

        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(user)));

        return Task.CompletedTask;
    }

    public Task NotifyUserLogout()
    {
        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(_anonymous)));

        return Task.CompletedTask;
    }
}
