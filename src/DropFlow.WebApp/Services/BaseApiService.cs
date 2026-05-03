using System.Net.Http.Headers;
using DropFlow.Shared.Common;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services;

/// <summary>
/// Classe de base pour tous les services API
/// Centralise la gestion du token JWT et HttpClient
/// </summary>
public abstract class BaseApiService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger logger)
{
    private const string TokenKey = "auth_token";

    protected readonly IHttpClientFactory HttpClientFactory = httpClientFactory;
    protected readonly ProtectedLocalStorage LocalStorage = localStorage;
    protected readonly ILogger Logger = logger;

    /// <summary>
    /// Récupère le token JWT depuis le local storage
    /// </summary>
    protected async Task<string?> GetTokenAsync()
    {
        try
        {
            var result = await LocalStorage.GetAsync<string>(TokenKey);
            return result.Success ? result.Value : null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting token from local storage");
            return null;
        }
    }

    /// <summary>
    /// Crée un HttpClient avec le header Authorization Bearer
    /// </summary>
    protected async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        var client = HttpClientFactory.CreateClient("DropFlowAPI");

        var token = await GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            Logger.LogWarning(
                "No JWT token found when creating authorized HttpClient");
        }

        return client;
    }

    // -------------------------
    // GET
    // -------------------------
    protected async Task<T?> GetAsync<T>(string endpoint) where T : class
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<T>();

            Logger.LogWarning(
                "GET {Endpoint} failed. Status: {StatusCode}",
                endpoint,
                response.StatusCode);

            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling GET {Endpoint}", endpoint);
            return null;
        }
    }

    protected async Task<T> GetValueAsync<T>(
        string endpoint,
        T defaultValue = default) where T : struct
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<T>();

            Logger.LogWarning(
                "GET {Endpoint} failed. Status: {StatusCode}",
                endpoint,
                response.StatusCode);

            return defaultValue;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling GET {Endpoint}", endpoint);
            return defaultValue;
        }
    }

    // -------------------------
    // POST
    // -------------------------
    protected async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request)
        where TResponse : class
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var response = await client.PostAsJsonAsync(endpoint, request);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<TResponse>();

            var error = await response.Content.ReadAsStringAsync();
            Logger.LogWarning(
                "POST {Endpoint} failed. Status: {StatusCode}, Error: {Error}",
                endpoint,
                response.StatusCode,
                error);

            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling POST {Endpoint}", endpoint);
            return null;
        }
    }

    protected async Task<ResponseResult> PostAsync(string endpoint)
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var response = await client.PostAsync(endpoint, null);

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("POST {Endpoint} succeeded", endpoint);
                return ResponseResult.Success();
            }

            var error = await response.Content.ReadAsStringAsync();
            Logger.LogWarning(
                "POST {Endpoint} failed. Status: {StatusCode}, Error: {Error}",
                endpoint,
                response.StatusCode,
                error);

            return ResponseResult.Failure(error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling POST {Endpoint}", endpoint);
            return ResponseResult.Failure(ex.Message);
        }
    }

    protected async Task<ResponseResult<TResponse>>
        PostWithResultAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request)
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            using var response = await client.PostAsJsonAsync(endpoint, request);

            if (response.IsSuccessStatusCode)
            {
                if (response.Content.Headers.ContentLength == 0)
                    return ResponseResult<TResponse>.Success(default!);

                var result =
                    await response.Content
                        .ReadFromJsonAsync<ResponseResult<TResponse>>();

                return result ??
                       ResponseResult<TResponse>.Failure(
                           "Réponse serveur invalide.");
            }

            var error = await response.Content.ReadAsStringAsync();
            Logger.LogWarning(
                "POST {Endpoint} failed. Status: {StatusCode}, Error: {Error}",
                endpoint,
                response.StatusCode,
                error);

            return ResponseResult<TResponse>.Failure(error);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP error calling POST {Endpoint}", endpoint);
            return ResponseResult<TResponse>.Failure(
                "Erreur de communication avec le serveur.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling POST {Endpoint}", endpoint);
            return ResponseResult<TResponse>.Failure(ex.Message);
        }
    }

    protected async Task<ResponseResult> PostAsync<TRequest>(
        string endpoint,
        TRequest request)
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var response = await client.PostAsJsonAsync(endpoint, request);

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("POST {Endpoint} succeeded", endpoint);
                return ResponseResult.Success();
            }

            var error = await response.Content.ReadAsStringAsync();
            Logger.LogWarning(
                "POST {Endpoint} failed. Status: {StatusCode}, Error: {Error}",
                endpoint,
                response.StatusCode,
                error);

            return ResponseResult.Failure(error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling POST {Endpoint}", endpoint);
            return ResponseResult.Failure(ex.Message);
        }
    }

    // -------------------------
    // PUT
    // -------------------------
    protected async Task<ResponseResult> PutAsync<TRequest>(
        string endpoint,
        TRequest request)
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var response = await client.PutAsJsonAsync(endpoint, request);

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("PUT {Endpoint} succeeded", endpoint);
                return ResponseResult.Success();
            }

            var error = await response.Content.ReadAsStringAsync();
            Logger.LogWarning(
                "PUT {Endpoint} failed. Status: {StatusCode}, Error: {Error}",
                endpoint,
                response.StatusCode,
                error);

            return ResponseResult.Failure(error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling PUT {Endpoint}", endpoint);
            return ResponseResult.Failure(ex.Message);
        }
    }

    protected async Task<ResponseResult> PutAsync(string endpoint)
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var response = await client.PutAsync(endpoint, null);

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("PUT {Endpoint} succeeded", endpoint);
                return ResponseResult.Success();
            }

            var error = await response.Content.ReadAsStringAsync();
            Logger.LogWarning(
                "PUT {Endpoint} failed. Status: {StatusCode}, Error: {Error}",
                endpoint,
                response.StatusCode,
                error);

            return ResponseResult.Failure(error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling PUT {Endpoint}", endpoint);
            return ResponseResult.Failure(ex.Message);
        }
    }

    // -------------------------
    // DELETE
    // -------------------------
    protected async Task<ResponseResult> DeleteAsync(string endpoint)
    {
        try
        {
            var client = await CreateAuthorizedClientAsync();
            var response = await client.DeleteAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("DELETE {Endpoint} succeeded", endpoint);
                return ResponseResult.Success();
            }

            var error = await response.Content.ReadAsStringAsync();
            Logger.LogWarning(
                "DELETE {Endpoint} failed. Status: {StatusCode}, Error: {Error}",
                endpoint,
                response.StatusCode,
                error);

            return ResponseResult.Failure(error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling DELETE {Endpoint}", endpoint);
            return ResponseResult.Failure(ex.Message);
        }
    }
}
