using DropFlow.Shared.Admin;
using DropFlow.Shared.Common;
using DropFlow.Shared.Profil;

namespace DropFlow.Application.Interfaces.Users;

public interface IAdminService
{
    // Tenants
    Task<List<TenantDto>> GetAllTenantsAsync();
    Task<TenantDetailsDto?> GetTenantDetailsAsync(int tenantId);
    Task<ResponseResult> ActivateTenantAsync(int tenantId);
    Task<ResponseResult> DeactivateTenantAsync(int tenantId);
    Task<ResponseResult> UpdateTenantPlanAsync(int tenantId, UpdateTenantPlanDto dto);
    Task<ResponseResult> DeleteTenantAsync(int tenantId);
    // Users
    Task<List<TenantUserDto>> GetTenantUsersAsync(int tenantId);
    Task<ResponseResult> ActivateUserAsync(int tenantId, string userId);
    Task<ResponseResult> DeactivateUserAsync(int tenantId, string userId);
    // Stats & Audit
    Task<GlobalStatsDto> GetGlobalStatsAsync();
    Task<List<AuditLogDto>> GetAuditLogsAsync(
        int? tenantId = null,
        string? userId = null,
        string? action = null,
        string? severity = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50);

    Task<List<UserProfileDto>> GetAllUsersAsync(
        int? tenantId = null,
        string? role = null,
        bool? isActive = null,
        string? searchTerm = null,
        bool includeDeactivated = false, 
        bool includeDeleted = false,
        int pageNumber = 1,
        int pageSize = 50);
    
    Task<UserStatsDto> GetUserStatsAsync();
    Task<ResponseResult> ActivateUserGlobalAsync(string userId);
    Task<ResponseResult> DeactivateUserGlobalAsync(string userId);
}