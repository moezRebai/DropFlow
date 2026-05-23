using DropFlow.Shared.Admin;
using DropFlow.Shared.Common;
using DropFlow.Shared.Profil;
using DropFlow.WebApp.Interfaces;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services.Users;

/// <summary>
/// Implémentation du service admin pour la gestion des tenants
/// Hérite de BaseApiService pour bénéficier de la gestion HTTP centralisée
/// </summary>
public class AdminService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger<AdminService> logger)
    : BaseApiService(httpClientFactory, localStorage, logger), IAdminService
{
    // ====== Tenants ======

    public async Task<List<TenantDto>> GetTenantsAsync()
        => await GetAsync<List<TenantDto>>("/api/admin/tenants") ?? new();

    public async Task<TenantDetailsDto?> GetTenantDetailsAsync(int tenantId)
        => await GetAsync<TenantDetailsDto>($"/api/admin/tenants/{tenantId}");

    public async Task<ResponseResult> ActivateTenantAsync(int tenantId)
        => await PostAsync($"/api/admin/tenants/{tenantId}/activate");

    public async Task<ResponseResult> DeactivateTenantAsync(int tenantId)
        => await PostAsync($"/api/admin/tenants/{tenantId}/deactivate");

    public async Task<ResponseResult> UpdateTenantPlanAsync(int tenantId, UpdateTenantPlanDto request)
        => await PutAsync($"/api/admin/tenants/{tenantId}/plan", request);

    public async Task<ResponseResult> DeleteTenantAsync(int tenantId)
        => await DeleteAsync($"/api/admin/tenants/{tenantId}");
    
    public async Task<List<TenantUserDto>> GetTenantUsersAsync(int tenantId)
        => await GetAsync<List<TenantUserDto>>($"/api/admin/tenants/{tenantId}/users") ?? new();

    public async Task<ResponseResult> ActivateTenantUserAsync(int tenantId, string userId)
        => await PostAsync($"/api/admin/tenants/{tenantId}/users/{userId}/activate");

    public async Task<ResponseResult> DeactivateTenantUserAsync(int tenantId, string userId)
        => await PostAsync($"/api/admin/tenants/{tenantId}/users/{userId}/deactivate");

    // ====== Statistiques ======

    public async Task<GlobalStatsDto?> GetStatsAsync()
        => await GetAsync<GlobalStatsDto>("/api/admin/stats");

    public async Task<List<AuditLogDto>> GetAuditLogsAsync(
        int? tenantId = null, 
        string? userId = null,
        string? action = null,
        string? severity = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1, 
        int pageSize = 50)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (tenantId.HasValue)
            queryParams.Add($"tenantId={tenantId.Value}");
        
        if (!string.IsNullOrWhiteSpace(userId))
            queryParams.Add($"userId={userId}");
        
        if (!string.IsNullOrWhiteSpace(action))
            queryParams.Add($"action={action}");
        
        if (!string.IsNullOrWhiteSpace(severity))
            queryParams.Add($"severity={severity}");
        
        if (startDate.HasValue)
            queryParams.Add($"startDate={startDate.Value:yyyy-MM-ddTHH:mm:ss}");
        
        if (endDate.HasValue)
            queryParams.Add($"endDate={endDate.Value:yyyy-MM-ddTHH:mm:ss}");

        var endpoint = $"/api/admin/audit?{string.Join("&", queryParams)}";
        
        return await GetAsync<List<AuditLogDto>>(endpoint) ?? [];
    }
    
    public async Task<List<UserProfileDto>> GetAllUsersAsync(
        int? tenantId = null,
        string? role = null,
        bool? isActive = null,
        string? searchTerm = null,
        bool includeDeactivated = false, 
        bool includeDeleted = false,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (tenantId.HasValue)
            queryParams.Add($"tenantId={tenantId.Value}");
        
        if (!string.IsNullOrWhiteSpace(role))
            queryParams.Add($"role={role}");
        
        if (includeDeactivated)
            queryParams.Add($"includeDeactivated={includeDeactivated}");
        
        if (includeDeleted)
            queryParams.Add($"includeDeleted={includeDeleted}");
        
        if (isActive.HasValue)
            queryParams.Add($"isActive={isActive.Value}");
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
            queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");

        var endpoint = $"/api/admin/users?{string.Join("&", queryParams)}";
        
        return await GetAsync<List<UserProfileDto>>(endpoint) ?? [];
    }
    
    public async Task<UserStatsDto?> GetUserStatsAsync()
        => await GetAsync<UserStatsDto>("/api/admin/users/stats");
    
    public async Task<ResponseResult> ActivateUserAsync(string userId)
        => await PostAsync($"/api/admin/users/{userId}/activate");
    
    public async Task<ResponseResult> DeactivateUserAsync(string userId)
        => await PostAsync($"/api/admin/users/{userId}/deactivate");
}