using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DropFlow.Mobile.Models;

namespace DropFlow.Mobile.Services;

public class SessionExpiredException : Exception
{
    public SessionExpiredException() : base("Session expirée") { }
}

public class NoConnectivityException : Exception
{
    public NoConnectivityException() : base("Pas de connexion Internet") { }
}

public class ApiService
{
    private readonly HttpClient _http;
    private readonly AuthStorageService _auth;
    private readonly ConnectivityService _connectivity;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Cache dashboard avec TTL 2 minutes
    private DashboardResponse? _cachedDashboard;
    private DateTime _dashboardCachedAt = DateTime.MinValue;
    private static readonly TimeSpan DashboardCacheTtl = TimeSpan.FromMinutes(2);

    public ApiService(AuthStorageService auth, ConnectivityService connectivity)
    {
        _auth = auth;
        _connectivity = connectivity;

        var handler = new HttpClientHandler();
#if DEBUG
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
#endif
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(GetBaseAddress()),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private static string GetBaseAddress()
    {
#if DEBUG
        return "https://10.0.2.2:7001";
#else
        return "https://dropflowapi.phonyx.net";
#endif
    }

    private void EnsureConnected()
    {
        if (!_connectivity.IsConnected)
            throw new NoConnectivityException();
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _auth.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new SessionExpiredException();

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        if (jwt.ValidTo <= DateTime.UtcNow.AddMinutes(1))
        {
            // JWT expiré — tenter un refresh si RememberMe est actif
            token = await TryRefreshAsync() ?? throw new SessionExpiredException();
        }

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<string?> TryRefreshAsync()
    {
        var refreshToken = await _auth.GetRefreshTokenAsync();
        if (string.IsNullOrEmpty(refreshToken)) return null;

        try
        {
            var body = JsonSerializer.Serialize(new { refreshToken });
            var response = await _http.PostAsync("api/auth/refresh",
                new StringContent(body, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode) return null;

            var result = await ReadAsync<LoginResponse>(response);
            if (!result.Success || string.IsNullOrEmpty(result.Token)) return null;

            // Sauvegarder le nouveau JWT (et le nouveau refresh token si présent)
            await _auth.SaveTokenAsync(result.Token);
            if (!string.IsNullOrEmpty(result.RefreshToken))
                await SecureStorage.Default.SetAsync("refresh_token", result.RefreshToken);

            return result.Token;
        }
        catch
        {
            return null;
        }
    }

    private async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _auth.Clear();
            throw new SessionExpiredException();
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions)
               ?? throw new InvalidOperationException("Réponse vide du serveur");
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        EnsureConnected();
        _http.DefaultRequestHeaders.Authorization = null;
        var json = JsonSerializer.Serialize(request);
        var response = await _http.PostAsync("api/auth/login",
            new StringContent(json, Encoding.UTF8, "application/json"));
        return await ReadAsync<LoginResponse>(response);
    }

    public async Task<DashboardResponse> GetDashboardAsync()
    {
        // Retourner le cache si offline ou si encore frais (< 2 min)
        if (_cachedDashboard != null)
        {
            var cacheAge = DateTime.UtcNow - _dashboardCachedAt;
            if (!_connectivity.IsConnected || cacheAge < DashboardCacheTtl)
                return _cachedDashboard;
        }

        if (!_connectivity.IsConnected)
            return new DashboardResponse
            {
                TodayRoute = new TodayRouteResponse { HasRoute = false, Message = "Pas de connexion Internet" }
            };

        await SetAuthHeaderAsync();
        var response = await _http.GetAsync("api/driver/dashboard");
        var result = await ReadAsync<DashboardResponse>(response);
        _cachedDashboard = result;
        _dashboardCachedAt = DateTime.UtcNow;
        return result;
    }

    public void InvalidateDashboardCache()
    {
        _dashboardCachedAt = DateTime.MinValue;
    }

    public async Task<TodayRouteResponse> GetRouteDetailAsync(int routeId)
    {
        EnsureConnected();
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/driver/routes/{routeId}");
        return await ReadAsync<TodayRouteResponse>(response);
    }

    public async Task<DeliveryDetailDto> GetDeliveryDetailAsync(int id)
    {
        EnsureConnected();
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/driver/deliveries/{id}");
        return await ReadAsync<DeliveryDetailDto>(response);
    }

    public async Task<ApiMessageResponse> ValidateDeliveryAsync(int id, ValidationRequest request)
    {
        EnsureConnected();
        await SetAuthHeaderAsync();
        var json = JsonSerializer.Serialize(request);
        var response = await _http.PostAsync($"api/driver/deliveries/{id}/validate",
            new StringContent(json, Encoding.UTF8, "application/json"));
        return await ReadAsync<ApiMessageResponse>(response);
    }

    public async Task<ApiMessageResponse> StartRouteAsync(int routeId)
    {
        EnsureConnected();
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/driver/route/{routeId}/start", null);
        var result = await ReadAsync<ApiMessageResponse>(response);
        InvalidateDashboardCache();
        return result;
    }

    public async Task<DeliveryHistoryResponse> GetDeliveryHistoryAsync(int page = 1, int pageSize = 20)
    {
        EnsureConnected();
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync($"api/driver/deliveries/history?page={page}&pageSize={pageSize}");
        return await ReadAsync<DeliveryHistoryResponse>(response);
    }

    public async Task<ApiMessageResponse> CompleteRouteAsync(int routeId)
    {
        EnsureConnected();
        await SetAuthHeaderAsync();
        var response = await _http.PostAsync($"api/driver/route/{routeId}/complete", null);
        var result = await ReadAsync<ApiMessageResponse>(response);
        InvalidateDashboardCache();
        return result;
    }
}
