using DropFlow.Shared.Common;
using DropFlow.Shared.Tenants;
using DropFlow.Shared.Tenants.Depots;

namespace DropFlow.Application.Interfaces;

/// <summary>
/// Service de gestion des informations entreprise et des dépôts
/// </summary>
public interface ITenantManagementService
{
    // ═══════════════════════════════════════════════════════════
    // TENANT INFO
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Récupère les informations du tenant courant
    /// </summary>
    Task<ResponseResult<TenantDto>> GetCurrentTenantAsync();
    
    /// <summary>
    /// Met à jour les informations générales de l'entreprise
    /// </summary>
    Task<ResponseResult> UpdateCompanyInfoAsync(UpdateTenantCompanyInfoDto dto);
    
    /// <summary>
    /// Met à jour les informations légales
    /// </summary>
    Task<ResponseResult> UpdateLegalInfoAsync(UpdateTenantLegalInfoDto dto);
    
    /// <summary>
    /// Met à jour le logo
    /// </summary>
    Task<ResponseResult> UpdateLogoAsync(UpdateTenantLogoDto dto);
    
    /// <summary>
    /// Supprime le logo
    /// </summary>
    Task<ResponseResult> RemoveLogoAsync();
    
    // ═══════════════════════════════════════════════════════════
    // DEPOTS
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Récupère tous les dépôts du tenant (pour dropdowns)
    /// </summary>
    Task<List<TenantDepotDto>> GetAllDepotsAsync();
    
    /// <summary>
    /// Récupère les dépôts avec pagination et filtres
    /// </summary>
    Task<PagedResult<TenantDepotDto>> GetDepotsAsync(TenantDepotFilterDto filter);
    
    /// <summary>
    /// Récupère un dépôt par son ID
    /// </summary>
    Task<ResponseResult<TenantDepotDto>> GetDepotByIdAsync(int id);
    
    /// <summary>
    /// Crée un nouveau dépôt
    /// </summary>
    Task<ResponseResult<int>> CreateDepotAsync(CreateTenantDepotDto dto);
    
    /// <summary>
    /// Met à jour un dépôt existant
    /// </summary>
    Task<ResponseResult> UpdateDepotAsync(int id, UpdateTenantDepotDto dto);
    
    /// <summary>
    /// Supprime un dépôt
    /// </summary>
    Task<ResponseResult> DeleteDepotAsync(int id);
    
    /// <summary>
    /// Définit un dépôt comme par défaut
    /// </summary>
    Task<ResponseResult> SetDefaultDepotAsync(int id);
    
    /// <summary>
    /// Active/Désactive un dépôt
    /// </summary>
    Task<ResponseResult> ToggleDepotStatusAsync(int id);
}