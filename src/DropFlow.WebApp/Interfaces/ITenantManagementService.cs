using DropFlow.Shared.Common;
using DropFlow.Shared.Tenants;
using DropFlow.Shared.Tenants.Depots;

namespace DropFlow.WebApp.Interfaces;

/// <summary>
/// Service de gestion des paramètres entreprise et dépôts (Frontend)
/// </summary>
public interface ITenantManagementService
{
    // ═══════════════════════════════════════════════════════════
    // TENANT INFO
    // ═══════════════════════════════════════════════════════════
    
    Task<TenantDto?> GetCurrentTenantAsync(bool forceRefresh = false);
    Task<ResponseResult> UpdateCompanyInfoAsync(UpdateTenantCompanyInfoDto dto);
    Task<ResponseResult> UpdateLegalInfoAsync(UpdateTenantLegalInfoDto dto);
    Task<ResponseResult> UpdateLogoAsync(UpdateTenantLogoDto dto);
    Task<ResponseResult> RemoveLogoAsync();
    
    // ═══════════════════════════════════════════════════════════
    // DEPOTS
    // ═══════════════════════════════════════════════════════════
    
    Task<List<TenantDepotDto>> GetAllDepotsAsync(bool forceRefresh = false);
    Task<PagedResult<TenantDepotDto>> GetDepotsAsync(TenantDepotFilterDto filter);
    Task<TenantDepotDto?> GetDepotByIdAsync(int id);
    Task<ResponseResult> CreateDepotAsync(CreateTenantDepotDto dto);
    Task<ResponseResult> UpdateDepotAsync(int id, UpdateTenantDepotDto dto);
    Task<ResponseResult> DeleteDepotAsync(int id);
    Task<ResponseResult> SetDefaultDepotAsync(int id);
    Task<ResponseResult> ToggleDepotStatusAsync(int id);
    
    // ═══════════════════════════════════════════════════════════
    // CACHE MANAGEMENT
    // ═══════════════════════════════════════════════════════════
    
    void InvalidateCache();
    void InvalidateDepotCache(int id);
    Task<TenantDto?> RefreshTenantAsync();
    Task<List<TenantDepotDto>> RefreshDepotsAsync();
}