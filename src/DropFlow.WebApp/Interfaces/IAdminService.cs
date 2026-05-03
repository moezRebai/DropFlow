using DropFlow.Shared.Admin;
using DropFlow.Shared.Common;
using DropFlow.Shared.Profil;

namespace DropFlow.WebApp.Interfaces;

/// <summary>
/// Service de gestion admin pour les tenants (Admin uniquement - TenantId = 0)
/// </summary>
public interface IAdminService
{
    // ====== Tenants ======
    
    /// <summary>
    /// Récupère la liste de tous les tenants
    /// </summary>
    Task<List<TenantDto>> GetTenantsAsync();
    
    /// <summary>
    /// Récupère les détails d'un tenant spécifique
    /// </summary>
    Task<TenantDetailsDto?> GetTenantDetailsAsync(int tenantId);
    
    /// <summary>
    /// Active un tenant
    /// </summary>
    Task<ResponseResult> ActivateTenantAsync(int tenantId);
    
    /// <summary>
    /// Désactive un tenant
    /// </summary>
    Task<ResponseResult> DeactivateTenantAsync(int tenantId);
    
    /// <summary>
    /// Met à jour le plan d'un tenant
    /// </summary>
    Task<ResponseResult> UpdateTenantPlanAsync(int tenantId, UpdateTenantPlanDto request);
    
    /// <summary>
    /// Supprime un tenant (soft delete)
    /// </summary>
    Task<ResponseResult> DeleteTenantAsync(int tenantId);
    
    // ====== Utilisateurs d'un tenant ======
    
    /// <summary>
    /// Récupère tous les utilisateurs d'un tenant
    /// </summary>
    Task<List<TenantUserDto>> GetTenantUsersAsync(int tenantId);
    
    /// <summary>
    /// Active un utilisateur d'un tenant
    /// </summary>
    Task<ResponseResult> ActivateTenantUserAsync(int tenantId, string userId);
    
    /// <summary>
    /// Désactive un utilisateur d'un tenant
    /// </summary>
    Task<ResponseResult> DeactivateTenantUserAsync(int tenantId, string userId);
    
    // ====== Statistiques ======
    
    /// <summary>
    /// Récupère les statistiques globales pour le dashboard admin
    /// </summary>
    Task<GlobalStatsDto?> GetStatsAsync();
    
    /// <summary>
    /// Récupère les logs d'audit avec filtres avancés
    /// </summary>
    Task<List<AuditLogDto>> GetAuditLogsAsync(
        int? tenantId = null,
        string? userId = null,
        string? action = null,
        string? severity = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 50);
    
    /// <summary>
    /// Récupère tous les utilisateurs de tous les tenants avec filtres
    /// </summary>
    Task<List<UserProfileDto>> GetAllUsersAsync(
        int? tenantId = null,
        string? role = null,
        bool? isActive = null,
        string? searchTerm = null,
        bool includeDeactivated = false,
        bool includeDeleted = false,
        int pageNumber = 1,
        int pageSize = 50);
    
    /// <summary>
    /// Récupère les statistiques globales des utilisateurs
    /// </summary>
    Task<UserStatsDto?> GetUserStatsAsync();
    
    /// <summary>
    /// Active un utilisateur (global)
    /// </summary>
    Task<ResponseResult> ActivateUserAsync(string userId);
    
    /// <summary>
    /// Désactive un utilisateur (global)
    /// </summary>
    Task<ResponseResult> DeactivateUserAsync(string userId);
}