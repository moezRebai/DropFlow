using DropFlow.Application.Interfaces.Emails;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Constants;
using DropFlow.Domain.Entities;
using DropFlow.Shared.Enums;
using DropFlow.Domain.Enums;
using DropFlow.Shared.Common;
using DropFlow.Shared.Profil;
using DropFlow.Shared.UserManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Application.Services.Users;

public class UserManagementService(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext context,
    ITenantService tenantService,
    IEmailService emailService,
    IAuditService auditService,
    ILogger<UserManagementService> logger)
    : IUserManagementService
{
    public async Task<ResponseResult> InviteUserAsync(InviteUserDto dto, string invitedBy)
    {
        try
        {
            var tenantId = tenantService.GetTenantId();

            if (dto.Role == Roles.Admin)
                return ResponseResult.Failure("Vous ne pouvez pas cr�er d'administrateur");

            // Accept "Driver" as frontend alias for "Livreur"
            if (dto.Role == "Driver") dto.Role = Roles.Livreur;

            if (!Roles.IsValid(dto.Role))
                return ResponseResult.Failure("R�le invalide");

            var existingUser = await context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == dto.Email &&
                    u.TenantId == tenantId &&
                    u.DeletedDate == null);

            if (existingUser != null)
                return ResponseResult.Failure("Un utilisateur avec cet email existe d�j�");

            var pendingInvitation = await context.UserInvitations
                .AnyAsync(i => i.Email == dto.Email &&
                               i.TenantId == tenantId &&
                               !i.IsUsed &&
                               i.ExpiresAt > DateTime.UtcNow);

            if (pendingInvitation)
                return ResponseResult.Failure("Une invitation est d�j� en cours pour cet email");

            var invitation = UserInvitation.Create(
                tenantId,
                dto.Email,
                dto.Role,
                invitedBy);

            context.UserInvitations.Add(invitation);
            await context.SaveChangesAsync();

            // Email d'invitation (non-bloquant — l'invitation est déjà persistée même si l'email échoue)
            try
            {
                await emailService.SendInvitationEmailAsync(
                    dto.Email,
                    invitation.Token,
                    tenantService.GetCurrentTenant()?.Name);
            }
            catch (Exception emailEx)
            {
                logger.LogWarning(emailEx, "Invitation email failed for {Email}, invitation still created", dto.Email);
            }

            await auditService.LogAsync(
                tenantId: tenantId,
                userId: invitedBy,
                action: AuditActions.UserInvited,
                entityName: nameof(UserInvitation),
                entityId: invitation.Id,
                changes: new { dto.Email, dto.Role },
                severity: AuditSeverity.Info);

            logger.LogInformation("User invited: {Email}", dto.Email);

            return ResponseResult.Success("Invitation envoy�e avec succ�s");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inviting user");
            return ResponseResult.Failure("Une erreur s'est produite");
        }
    }
    public async Task<ResponseResult> DeactivateUserAsync(string userId, string deactivatedBy)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return ResponseResult.Failure("Utilisateur introuvable");

            if (user.Id == deactivatedBy)
                return ResponseResult.Failure("Vous ne pouvez pas d�sactiver votre propre compte");

            var roles = await userManager.GetRolesAsync(user);
            if (roles.Contains(Roles.Manager))
            {
                var activeManagers = await context.Users
                    .Where(u => u.TenantId == user.TenantId &&
                                u.IsActive &&
                                u.Id != userId)
                    .ToListAsync();

                var managerCount = 0;
                foreach (var manager in activeManagers)
                {
                    var managerRoles = await userManager.GetRolesAsync(manager);
                    if (managerRoles.Contains(Roles.Manager))
                        managerCount++;
                }

                if (managerCount == 0)
                    return ResponseResult.Failure("Vous ne pouvez pas d�sactiver le dernier Manager");
            }

            user.Deactivate();
            await userManager.UpdateAsync(user);

            await auditService.LogAsync(
                tenantId: user.TenantId,
                userId: deactivatedBy,
                action: AuditActions.UserDeactivated,
                entityName: nameof(ApplicationUser),
                changes: new { UserId = userId, user.Email },
                severity: AuditSeverity.Warning);

            logger.LogInformation("User deactivated: {UserId}", userId);

            return ResponseResult.Success("Utilisateur d�sactiv� avec succ�s");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deactivating user");
            return ResponseResult.Failure("Une erreur s'est produite");
        }
    }
    public async Task<ResponseResult> ReactivateUserAsync(string userId, string reactivatedBy)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return ResponseResult.Failure("Utilisateur introuvable");

            user.Activate();
            await userManager.UpdateAsync(user);

            await auditService.LogAsync(
                tenantId: user.TenantId,
                userId: reactivatedBy,
                action: AuditActions.UserReactivated,
                entityName: nameof(ApplicationUser),
                changes: new { UserId = userId, user.Email },
                severity: AuditSeverity.Info);

            logger.LogInformation("User reactivated: {UserId}", userId);

            return ResponseResult.Success("Utilisateur r�activ� avec succ�s");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reactivating user");
            return ResponseResult.Failure("Une erreur s'est produite");
        }
    }
    public async Task<List<UserProfileDto>> GetTenantUsersAsync(int tenantId,
        bool includeDeactivated = false,
        bool includeDeleted = false)
    {
        var query = context.Users
            .Where(u => u.TenantId == tenantId);

        if (!includeDeleted)
        {
            query = query.Where(u => u.DeletedDate == null);
        }

        if (!includeDeactivated)
        {
            query = query.Where(u => u.IsActive);
        }

        var users = await query
            .OrderByDescending(u => u.CreatedDate)
            .ToListAsync();

        var result = new List<UserProfileDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);

            var tenantName = "DropFlow Platform";
            if (user.TenantId != TenantIds.DropFlowAdmin)
            {
                var tenant = await context.Tenants.FindAsync(user.TenantId);
                tenantName = tenant?.Name ?? "Unknown";
            }

            result.Add(new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber!,
                Address = user.Address!,
                Role = roles.FirstOrDefault() ?? string.Empty,
                TenantId = user.TenantId,
                TenantName = tenantName,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate,
                DeletedDate = user.DeletedDate
            });
        }

        return result;
    }
    public async Task<ResponseResult> ChangeUserRoleAsync(string userId, string newRole)
    {
        try
        {
            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            var targetUser = await userManager.FindByIdAsync(userId);
            if (targetUser == null)
                return ResponseResult.Failure("Target user not found");

            if (currentUser.TenantId != TenantIds.DropFlowAdmin)
            {
                if (targetUser.TenantId != currentUser.TenantId)
                    return ResponseResult.Failure("You can only manage users from your tenant");
            }
            else
            {
                if (targetUser.TenantId == TenantIds.DropFlowAdmin)
                    return ResponseResult.Failure("Cannot modify DropFlow Admin user");
            }

            // Accept both "Livreur" (backend constant) and "Driver" (frontend alias)
            if (newRole == "Driver") newRole = Roles.Livreur;

            var validRoles = new[] { Roles.Manager, Roles.Livreur, Roles.Accountant, Roles.ReadOnly };
            if (!validRoles.Contains(newRole))
                return ResponseResult.Failure($"Invalid role. Valid roles: {string.Join(", ", validRoles)}");

            var currentRoles = await userManager.GetRolesAsync(targetUser);
            var oldRole = currentRoles.FirstOrDefault() ?? "None";

            if (oldRole == newRole)
                return ResponseResult.Failure("User already has this role");

            if (currentRoles.Any())
            {
                var removeResult = await userManager.RemoveFromRolesAsync(targetUser, currentRoles);
                if (!removeResult.Succeeded)
                    return ResponseResult.Failure("Failed to remove old roles");
            }

            var addResult = await userManager.AddToRoleAsync(targetUser, newRole);
            if (!addResult.Succeeded)
                return ResponseResult.Failure(string.Join(", ", addResult.Errors.Select(e => e.Description)));

            // When a driver is promoted to another role, remove the Driver entity
            // so they no longer appear in the drivers list
            if (oldRole == Roles.Livreur && newRole != Roles.Livreur)
            {
                var driver = await context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
                if (driver != null)
                {
                    context.Drivers.Remove(driver);
                    await context.SaveChangesAsync();
                }
            }

            await auditService.LogAsync(
                tenantId: currentUser.TenantId,
                userId: currentUser.Id,
                action: "UserRoleChanged",
                entityName: nameof(ApplicationUser),
                entityId: targetUser.TenantId,
                changes: new
                {
                    TargetUserId = userId,
                    TargetUserEmail = targetUser.Email,
                    OldRole = oldRole,
                    NewRole = newRole
                },
                severity: AuditSeverity.Warning
            );

            logger.LogInformation(
                "User {UserId} changed role of {TargetUserId} from {OldRole} to {NewRole}",
                currentUser.Id, userId, oldRole, newRole);

            return ResponseResult.Success("User role changed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing user role");
            return ResponseResult.Failure("An error occurred while changing user role");
        }
    }
    public async Task<ResponseResult> DeleteUserAsync(string userId)
    {
        try
        {
            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return ResponseResult.Failure("Target user not found");

            // V�rifier m�me tenant
            if (user.TenantId != currentUser.TenantId &&
                currentUser.TenantId != TenantIds.DropFlowAdmin)
            {
                return ResponseResult.Failure("You can only delete users from your tenant");
            }

            // Ne pas supprimer soi-m�me
            if (user.Id == currentUser.Id)
                return ResponseResult.Failure("You cannot delete yourself");

            // ? V�rifier livraisons en cours
            // var activeDeliveries = await context.Deliveries
            //     .Where(d => d.DriverId == userId && 
            //                 d.Status != DeliveryStatus.Done &&
            //                 d.Status != DeliveryStatus.Canceled)
            //     .CountAsync();
            //
            // if (activeDeliveries > 0)
            // {
            //     return ResponseResult.Failure(
            //         $"Cannot delete user with {activeDeliveries} active deliveries. " +
            //         "Please reassign or complete them first.");
            // }

            user.SoftDelete();
            await userManager.UpdateAsync(user);

            // Audit
            await auditService.LogAsync(
                tenantId: currentUser.TenantId,
                userId: currentUser.Id,
                action: "UserDeleted",
                entityName: nameof(ApplicationUser),
                changes: new
                {
                    DeletedUserId = userId,
                    DeletedUserEmail = user.Email
                },
                severity: AuditSeverity.Warning
            );

            logger.LogInformation(
                "User {UserId} deleted user {DeletedUserId}",
                currentUser.Id, userId);

            return ResponseResult.Success("User deleted successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting user");
            return ResponseResult.Failure("An error occurred while deleting user");
        }
    }
    public async Task<ResponseResult> RestoreUserAsync(string userId)
    {
        try
        {
            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            // ? IMPORTANT : IgnoreQueryFilters pour voir les supprim�s
            var user = await context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId);
        
            if (user == null)
                return ResponseResult.Failure("User not found");

            if (user.DeletedDate == null)
                return ResponseResult.Failure("User is not deleted");

            // V�rifier permissions
            if (user.TenantId != currentUser.TenantId && 
                currentUser.TenantId != TenantIds.DropFlowAdmin)
            {
                return ResponseResult.Failure("Access denied");
            }

            // ? Restore
            user.Restore();
            
            await userManager.UpdateAsync(user);

            // Audit
            await auditService.LogAsync(
                tenantId: currentUser.TenantId,
                userId: currentUser.Id,
                action: "UserRestored",
                entityName: nameof(ApplicationUser),
                changes: new 
                { 
                    RestoredUserId = userId, 
                    RestoredUserEmail = user.Email 
                },
                severity: AuditSeverity.Critical
            );

            logger.LogInformation(
                "User {UserId} restored user {RestoredUserId}", 
                currentUser.Id, userId);

            return ResponseResult.Success("User restored successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error restoring user");
            return ResponseResult.Failure("An error occurred while restoring user");
        }
    }
}