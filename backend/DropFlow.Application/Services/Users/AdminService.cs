using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Constants;
using DropFlow.Domain.Entities;
using DropFlow.Shared.Enums;
using DropFlow.Domain.Enums;
using DropFlow.Shared.Admin;
using DropFlow.Shared.Common;
using DropFlow.Shared.Profil;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Application.Services.Users;

public class AdminService(
    IApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ITenantService tenantService,
    IAuditService auditService,
    ILogger<AdminService> logger)
    : IAdminService
{
    // ----------------------------------------------------------------
    // TENANTS
    // ----------------------------------------------------------------

    public async Task<List<TenantDto>> GetAllTenantsAsync()
    {
        // Vérifier que l'utilisateur est Admin DropFlow
        if (!tenantService.IsDropFlowAdmin())
            throw new UnauthorizedAccessException("Access denied");

        var tenants = await context.Tenants
            .OrderByDescending(t => t.CreatedDate)
            .Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                SubDomain = t.SubDomain,
                PlanType = t.PlanType,
                MaxUsers = t.MaxUsers,
                MaxDeliveries = t.MaxDeliveries,
                IsActive = t.IsActive,
                CreatedDate = t.CreatedDate,
                ExpiryDate = t.ExpiryDate,

                UserCount = context.Users.Count(u => u.TenantId == t.Id),
                ActiveUserCount = context.Users.Count(u => u.TenantId == t.Id && u.IsActive),

                LastActivityDate = context.Users
                    .Where(u => u.TenantId == t.Id)
                    .Select(u => u.LastLoginDate)
                    .Max()
            })
            .ToListAsync();

        return tenants;
    }
    public async Task<TenantDetailsDto?> GetTenantDetailsAsync(int tenantId)
    {
        if (!tenantService.IsDropFlowAdmin())
            throw new UnauthorizedAccessException("Access denied");

        var tenant = await context.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return null;

        var users = await context.Users
            .Where(u => u.TenantId == tenantId)
            .OrderByDescending(u => u.CreatedDate)
            .ToListAsync();

        var recentUsers = new List<TenantUserDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            recentUsers.Add(new TenantUserDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                roles.FirstOrDefault() ?? "User",
                user.IsActive,
                user.CreatedDate,
                user.LastLoginDate,
                tenant.Id,
                tenant.Name
            ));
        }

        var userCount = await context.Users.CountAsync(u => u.TenantId == tenantId);
        var activeUserCount = await context.Users.CountAsync(u => u.TenantId == tenantId && u.IsActive);

        return new TenantDetailsDto(recentUsers)
        {
            Id = tenant.Id,
            Name = tenant.Name,
            SubDomain = tenant.SubDomain,
            PlanType = tenant.PlanType,
            MaxUsers = tenant.MaxUsers,
            MaxDeliveries = tenant.MaxDeliveries,
            IsActive = tenant.IsActive,
            CreatedDate = tenant.CreatedDate,
            ExpiryDate = tenant.ExpiryDate,
            UserCount = userCount,
            ActiveUserCount = activeUserCount,
        };
    }
    public async Task<ResponseResult> ActivateTenantAsync(int tenantId)
    {
        if (!tenantService.IsDropFlowAdmin())
            return ResponseResult.Failure("Access denied");

        var tenant = await context.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return ResponseResult.Failure("Tenant not found");

        tenant.Activate();
        await context.SaveChangesAsync();

        var adminId = tenantService.GetCurrentUser()?.Id;
        await auditService.LogAsync(
            tenantId: TenantIds.DropFlowAdmin,
            userId: adminId,
            action: "TenantActivated",
            entityName: nameof(Tenant),
            entityId: tenantId,
            changes: new { TenantId = tenantId, TenantName = tenant.Name },
            severity: AuditSeverity.Critical
        );

        logger.LogInformation("Tenant {TenantId} activated by admin {AdminId}", tenantId, adminId);

        return ResponseResult.Success("Tenant activated successfully");
    }
    public async Task<ResponseResult> DeactivateTenantAsync(int tenantId)
    {
        if (!tenantService.IsDropFlowAdmin())
            return ResponseResult.Failure("Access denied");

        var tenant = await context.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return ResponseResult.Failure("Tenant not found");

        tenant.Deactivate();
        await context.SaveChangesAsync();

        var adminId = tenantService.GetCurrentUser()?.Id;
        await auditService.LogAsync(
            tenantId: TenantIds.DropFlowAdmin,
            userId: adminId,
            action: "TenantDeactivated",
            entityName: nameof(Tenant),
            entityId: tenantId,
            changes: new { TenantId = tenantId, TenantName = tenant.Name },
            severity: AuditSeverity.Critical
        );

        logger.LogInformation("Tenant {TenantId} deactivated by admin {AdminId}", tenantId, adminId);

        return ResponseResult.Success("Tenant deactivated successfully");
    }
    public async Task<ResponseResult> UpdateTenantPlanAsync(int tenantId, UpdateTenantPlanDto dto)
    {
        if (!tenantService.IsDropFlowAdmin())
            return ResponseResult.Failure("Access denied");

        var tenant = await context.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return ResponseResult.Failure("Tenant not found");

        var oldPlan = tenant.PlanType;
        var oldMaxUsers = tenant.MaxUsers;
        var oldMaxDeliveries = tenant.MaxDeliveries;

        tenant.UpdatePlan(dto.PlanType, dto.MaxUsers, dto.MaxDeliveries);

        if (dto.ExpiryDate.HasValue)
        {
            // Utiliser réflexion pour modifier ExpiryDate (propriété privée)
            var property = tenant.GetType().GetProperty("ExpiryDate");
            property?.SetValue(tenant, dto.ExpiryDate.Value);
        }

        await context.SaveChangesAsync();

        var adminId = tenantService.GetCurrentUser()?.Id;
        await auditService.LogAsync(
            tenantId: TenantIds.DropFlowAdmin,
            userId: adminId,
            action: "TenantPlanUpdated",
            entityName: nameof(Tenant),
            entityId: tenantId,
            changes: new
            {
                TenantId = tenantId,
                TenantName = tenant.Name,
                OldPlan = oldPlan,
                NewPlan = dto.PlanType,
                OldMaxUsers = oldMaxUsers,
                NewMaxUsers = dto.MaxUsers,
                OldMaxDeliveries = oldMaxDeliveries,
                NewMaxDeliveries = dto.MaxDeliveries
            },
            severity: AuditSeverity.Critical
        );

        logger.LogInformation("Tenant {TenantId} plan updated by admin {AdminId}", tenantId, adminId);

        return ResponseResult.Success("Tenant plan updated successfully");
    }
    public async Task<ResponseResult> DeleteTenantAsync(int tenantId)
    {
        if (!tenantService.IsDropFlowAdmin())
            return ResponseResult.Failure("Access denied");

        var tenant = await context.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return ResponseResult.Failure("Tenant not found");

        // Soft delete : Désactiver au lieu de supprimer
        tenant.Deactivate();
        await context.SaveChangesAsync();

        var adminId = tenantService.GetCurrentUser()?.Id;
        await auditService.LogAsync(
            tenantId: TenantIds.DropFlowAdmin,
            userId: adminId,
            action: "TenantDeleted",
            entityName: nameof(Tenant),
            entityId: tenantId,
            changes: new { TenantId = tenantId, TenantName = tenant.Name },
            severity: AuditSeverity.Critical
        );

        logger.LogWarning("Tenant {TenantId} deleted by admin {AdminId}", tenantId, adminId);
        return ResponseResult.Success("Tenant deleted successfully");
    }
    public async Task<List<TenantUserDto>> GetTenantUsersAsync(int tenantId)
    {
        if (!tenantService.IsDropFlowAdmin())
            throw new UnauthorizedAccessException("Access denied");

        var tenant = await context.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return new List<TenantUserDto>();

        var users = await context.Users
            .Where(u => u.TenantId == tenantId)
            .OrderByDescending(u => u.CreatedDate)
            .ToListAsync();

        var userDtos = new List<TenantUserDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            userDtos.Add(new TenantUserDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                roles.FirstOrDefault() ?? "User",
                user.IsActive,
                user.CreatedDate,
                user.LastLoginDate,
                tenant.Id,
                tenant.Name
            ));
        }

        return userDtos;
    }
    public async Task<ResponseResult> ActivateUserAsync(int tenantId, string userId)
    {
        if (!tenantService.IsDropFlowAdmin())
            return ResponseResult.Failure("Access denied");

        var user = await userManager.FindByIdAsync(userId);
        if (user == null || user.TenantId != tenantId)
            return ResponseResult.Failure("User not found");

        user.Activate();
        await userManager.UpdateAsync(user);

        var adminId = tenantService.GetCurrentUser()?.Id;
        await auditService.LogAsync(
            tenantId: TenantIds.DropFlowAdmin,
            userId: adminId,
            action: "UserActivatedByAdmin",
            entityName: nameof(ApplicationUser),
            changes: new { UserId = userId, UserEmail = user.Email, TenantId = tenantId },
            severity: AuditSeverity.Warning
        );

        return ResponseResult.Success("User activated successfully");
    }
    public async Task<ResponseResult> DeactivateUserAsync(int tenantId, string userId)
    {
        if (!tenantService.IsDropFlowAdmin())
            return ResponseResult.Failure("Access denied");

        var user = await userManager.FindByIdAsync(userId);
        if (user == null || user.TenantId != tenantId)
            return ResponseResult.Failure("User not found");

        user.Deactivate();
        await userManager.UpdateAsync(user);

        var adminId = tenantService.GetCurrentUser()?.Id;
        await auditService.LogAsync(
            tenantId: TenantIds.DropFlowAdmin,
            userId: adminId,
            action: "UserDeactivatedByAdmin",
            entityName: nameof(ApplicationUser),
            changes: new { UserId = userId, UserEmail = user.Email, TenantId = tenantId },
            severity: AuditSeverity.Warning
        );

        return ResponseResult.Success("User deactivated successfully");
    }
    public async Task<GlobalStatsDto> GetGlobalStatsAsync()
    {
        if (!tenantService.IsDropFlowAdmin())
            throw new UnauthorizedAccessException("Access denied");

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfWeek = now.AddDays(-(int)now.DayOfWeek);

        var totalTenants = await context.Tenants.CountAsync();
        var activeTenants = await context.Tenants.CountAsync(t => t.IsActive);
        var inactiveTenants = totalTenants - activeTenants;

        var totalUsers = await context.Users.CountAsync(u => u.TenantId > 0);
        var activeUsers = await context.Users.CountAsync(u => u.TenantId > 0 && u.IsActive);

        var tenantsThisMonth = await context.Tenants
            .CountAsync(t => t.CreatedDate >= startOfMonth);
        var tenantsThisWeek = await context.Tenants
            .CountAsync(t => t.CreatedDate >= startOfWeek);

        var usersThisMonth = await context.Users
            .CountAsync(u => u.TenantId > 0 && u.CreatedDate >= startOfMonth);

        var tenantsByPlan = await context.Tenants
            .GroupBy(t => t.PlanType)
            .Select(g => new { Plan = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Plan, x => x.Count);

        return new GlobalStatsDto(
            totalTenants,
            activeTenants,
            inactiveTenants,
            totalUsers,
            activeUsers,
            tenantsThisMonth,
            tenantsThisWeek,
            usersThisMonth,
            tenantsByPlan
        );
    }
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
        // Vérification sécurité
        if (!tenantService.IsDropFlowAdmin())
            throw new UnauthorizedAccessException("Access denied");

        // Construction de la requête de base
        var query = context.AuditLogs.AsQueryable();

        // ===== APPLICATION DES FILTRES =====

        // Filtre par Tenant
        if (tenantId.HasValue)
            query = query.Where(a => a.TenantId == tenantId.Value);

        // Filtre par Utilisateur
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserId == userId);

        // Filtre par Action
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        // Filtre par Sévérité
        if (!string.IsNullOrWhiteSpace(severity))
        {
            // Convertir le string en enum si nécessaire
            if (Enum.TryParse<AuditSeverity>(severity, out var severityEnum))
                query = query.Where(a => a.Severity == severityEnum);
        }

        // Filtre par Date Début
        if (startDate.HasValue)
            query = query.Where(a => a.Timestamp >= startDate.Value);

        // Filtre par Date Fin
        if (endDate.HasValue)
            query = query.Where(a => a.Timestamp <= endDate.Value);

        // ===== TRI ET PAGINATION =====

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // ===== CONSTRUCTION DES DTOs =====

        var logDtos = new List<AuditLogDto>();

        foreach (var log in logs)
        {
            string? tenantName = null;
            if (log.TenantId > 0)
            {
                var tenant = await context.Tenants.FindAsync(log.TenantId);
                tenantName = tenant?.Name;
            }

            string? userEmail = null;
            if (!string.IsNullOrEmpty(log.UserId))
            {
                var user = await userManager.FindByIdAsync(log.UserId);
                userEmail = user?.Email;
            }

            logDtos.Add(new AuditLogDto(
                log.Id,
                log.TenantId,
                tenantName,
                log.UserId,
                userEmail ?? "Utilisateur inconnu",
                log.Action,
                log.EntityName,
                log.EntityId,
                log.Changes,
                log.Severity.ToString(),
                log.Timestamp
            ));
        }

        return logDtos;
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
        if (!tenantService.IsDropFlowAdmin())
            throw new UnauthorizedAccessException("Access denied");

        // Validation pagination
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 100) pageSize = 100; // Max 100 par page

        // Query de base - TOUS les users (sauf Admin DropFlow lui-même)
        var query = context.Users
            .Where(u => u.TenantId > 0) // Exclure Admin DropFlow
            .AsQueryable();

        // Filtres
        if (tenantId.HasValue)
            query = query.Where(u => u.TenantId == tenantId.Value);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.ToLower();
            query = query.Where(u =>
                u.Email!.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                u.FirstName.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                u.LastName.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search, StringComparison.CurrentCultureIgnoreCase)));
        }

        if (!includeDeleted)
        {
            query = query.Where(u => u.DeletedDate == null);
        }

        if (!includeDeactivated)
        {
            query = query.Where(u => u.IsActive);
        }
        
        // Pagination
        var users = await query
            .OrderByDescending(u => u.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Conversion en DTOs
        var userDtos = new List<UserProfileDto>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "User";

            // Filtrer par rôle si demandé
            if (!string.IsNullOrWhiteSpace(role) && userRole != role)
                continue;

            var tenantName = "Unknown";
            var tenant = await context.Tenants.FindAsync(user.TenantId);
            if (tenant != null)
                tenantName = tenant.Name;

            userDtos.Add(new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Address = user.Address ?? string.Empty,
                Role = userRole,
                TenantId = user.TenantId,
                TenantName = tenantName,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate
            });
        }

        return userDtos;
    }

    public async Task<UserStatsDto> GetUserStatsAsync()
    {
        if (!tenantService.IsDropFlowAdmin())
            throw new UnauthorizedAccessException("Access denied");

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfWeek = now.AddDays(-(int)now.DayOfWeek);

        // Stats globales (exclure Admin DropFlow)
        var totalUsers = await context.Users.CountAsync(u => u.TenantId > 0);
        var activeUsers = await context.Users.CountAsync(u => u.TenantId > 0 && u.IsActive);
        var inactiveUsers = totalUsers - activeUsers;

        var usersThisMonth = await context.Users
            .CountAsync(u => u.TenantId > 0 && u.CreatedDate >= startOfMonth);

        var usersThisWeek = await context.Users
            .CountAsync(u => u.TenantId > 0 && u.CreatedDate >= startOfWeek);

        // Users par rôle
        var usersByRole = new Dictionary<string, int>();
        var allUsers = await context.Users.Where(u => u.TenantId > 0).ToListAsync();

        foreach (var user in allUsers)
        {
            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";

            if (usersByRole.ContainsKey(role))
                usersByRole[role]++;
            else
                usersByRole[role] = 1;
        }

        // Users par tenant
        var usersByTenant = await context.Users
            .Where(u => u.TenantId > 0)
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count);

        return new UserStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            InactiveUsers = inactiveUsers,
            UsersCreatedThisMonth = usersThisMonth,
            UsersCreatedThisWeek = usersThisWeek,
            UsersByRole = usersByRole,
            UsersByTenant = usersByTenant
        };
    }
    public async Task<ResponseResult> ActivateUserGlobalAsync(string userId)
    {
        if (!tenantService.IsDropFlowAdmin())
            return ResponseResult.Failure("Access denied");

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return ResponseResult.Failure("User not found");

        if (user.TenantId == TenantIds.DropFlowAdmin)
            return ResponseResult.Failure("Cannot modify DropFlow Admin user");

        user.Activate();
        await userManager.UpdateAsync(user);

        var adminId = tenantService.GetCurrentUser()?.Id;
        await auditService.LogAsync(
            tenantId: TenantIds.DropFlowAdmin,
            userId: adminId,
            action: "UserActivatedGlobal",
            entityName: nameof(ApplicationUser),
            changes: new { UserId = userId, UserEmail = user.Email, user.TenantId },
            severity: AuditSeverity.Warning
        );

        logger.LogInformation("Admin activated user {UserId} globally", userId);

        return ResponseResult.Success("User activated successfully");
    }
    public async Task<ResponseResult> DeactivateUserGlobalAsync(string userId)
    {
        if (!tenantService.IsDropFlowAdmin())
            return ResponseResult.Failure("Access denied");

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return ResponseResult.Failure("User not found");

        if (user.TenantId == TenantIds.DropFlowAdmin)
            return ResponseResult.Failure("Cannot modify DropFlow Admin user");

        user.Deactivate();
        await userManager.UpdateAsync(user);

        var adminId = tenantService.GetCurrentUser()?.Id;
        await auditService.LogAsync(
            tenantId: TenantIds.DropFlowAdmin,
            userId: adminId,
            action: "UserDeactivatedGlobal",
            entityName: nameof(ApplicationUser),
            changes: new { UserId = userId, UserEmail = user.Email, user.TenantId },
            severity: AuditSeverity.Warning
        );

        logger.LogInformation("Admin deactivated user {UserId} globally", userId);

        return ResponseResult.Success("User deactivated successfully");
    }
}