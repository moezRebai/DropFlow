using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Constants;
using DropFlow.Domain.Entities;
using DropFlow.Shared.Enums;
using DropFlow.Domain.Enums;
using DropFlow.Shared.Common;
using DropFlow.Shared.Profil;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Application.Services.Users;

public class ProfileService(
    IApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ITenantService tenantService,
    IAuditService auditService,
    ILogger<ProfileService> logger)
    : IProfileService
{
    // ----------------------------------------------------------------
    // PROFILE
    // ----------------------------------------------------------------

    public async Task<UserProfileDto?> GetCurrentUserProfileAsync()
    {
        var currentUser = tenantService.GetCurrentUser();
        if (currentUser == null)
            return null;

        var roles = await userManager.GetRolesAsync(currentUser);
        var role = roles.FirstOrDefault() ?? "User";

        var tenantName = "DropFlow Platform";
        if (currentUser.TenantId != TenantIds.DropFlowAdmin)
        {
            var tenant = await context.Tenants.FindAsync(currentUser.TenantId);
            tenantName = tenant?.Name ?? "Unknown";
        }

        return new UserProfileDto
        {
            Id = currentUser.Id,
            Email = currentUser.Email!,
            FirstName = currentUser.FirstName,
            LastName = currentUser.LastName,
            PhoneNumber = currentUser.PhoneNumber!,
            Address = currentUser.Address!,
            Role = role,
            TenantId = currentUser.TenantId,
            TenantName = tenantName,
            IsActive = currentUser.IsActive,
            CreatedDate = currentUser.CreatedDate,
            LastLoginDate = currentUser.LastLoginDate
        };

    }

    public async Task<ResponseResult> UpdateProfileAsync(UpdateProfileDto dto)
    {
        try
        {
            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            var user = await userManager.FindByIdAsync(currentUser.Id);
            if (user == null)
                return ResponseResult.Failure("User not found");

            // Validation
            if (string.IsNullOrWhiteSpace(dto.FirstName))
                return ResponseResult.Failure("First name is required");

            if (string.IsNullOrWhiteSpace(dto.LastName))
                return ResponseResult.Failure("Last name is required");

            var oldValues = new
            {
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.Address
            };

            user.UpdateProfile(dto.FirstName, dto.LastName, dto.PhoneNumber, dto.Address);
            
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return ResponseResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

            // Audit
            await auditService.LogAsync(
                tenantId: user.TenantId,
                userId: user.Id,
                action: "ProfileUpdated",
                entityName: nameof(ApplicationUser),
                changes: new
                {
                    Old = oldValues,
                    New = new { dto.FirstName, dto.LastName, dto.PhoneNumber, dto.Address }
                },
                severity: AuditSeverity.Info
            );

            logger.LogInformation("User {UserId} updated profile", user.Id);

            return ResponseResult.Success("Profile updated successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating profile");
            return ResponseResult.Failure("An error occurred while updating profile");
        }
    }

    // ----------------------------------------------------------------
    // PASSWORD
    // ----------------------------------------------------------------

    public async Task<ResponseResult> ChangePasswordAsync(ChangePasswordDto dto)
    {
        try
        {
            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            var user = await userManager.FindByIdAsync(currentUser.Id);
            if (user == null)
                return ResponseResult.Failure("User not found");

            // Validation
            if (dto.NewPassword != dto.ConfirmNewPassword)
                return ResponseResult.Failure("New passwords do not match");

            if (dto.NewPassword == dto.CurrentPassword)
                return ResponseResult.Failure("New password must be different from current password");

            // Vérifier mot de passe actuel
            var isCurrentPasswordValid = await userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                await auditService.LogAsync(
                    tenantId: user.TenantId,
                    userId: user.Id,
                    action: "PasswordChangeFailed",
                    entityName: nameof(ApplicationUser),
                    changes: new { Reason = "Invalid current password" },
                    severity: AuditSeverity.Warning
                );

                return ResponseResult.Failure("Current password is incorrect");
            }

            // Changer mot de passe
            var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return ResponseResult.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));

            // Audit
            await auditService.LogAsync(
                tenantId: user.TenantId,
                userId: user.Id,
                action: "PasswordChanged",
                entityName: nameof(ApplicationUser),
                severity: AuditSeverity.Warning
            );

            logger.LogInformation("User {UserId} changed password", user.Id);

            return ResponseResult.Success("Password changed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing password");
            return ResponseResult.Failure("An error occurred while changing password");
        }
    }

    // ----------------------------------------------------------------
    // PREFERENCES
    // ----------------------------------------------------------------

    public async Task<UserPreferencesDto> GetPreferencesAsync()
    {
        var currentUser = tenantService.GetCurrentUser();
        if (currentUser == null)
            throw new UnauthorizedAccessException("User not found");

        var preferences = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

        if (preferences == null)
        {
            // Créer préférences par défaut
            preferences = UserPreferences.CreateDefault(currentUser.Id);
            context.UserPreferences.Add(preferences);
            await context.SaveChangesAsync();
        }

        return new UserPreferencesDto
        {
            Id = preferences.Id,
            UserId = preferences.UserId,
            EmailNotificationsEnabled = preferences.EmailNotificationsEnabled,
            EmailOnNewDelivery = false,
            EmailOnDeliveryCompleted = false,
            EmailOnInvoiceCreated = false,
            EmailOnInvoicePaid = false,
            SmsNotificationsEnabled = preferences.EmailNotificationsEnabled,
            SmsOnUrgentDelivery = false,
            SmsOnDeliveryLate = false,
            InAppNotificationsEnabled = preferences.SystemNotifications,
            DarkModeEnabled = false,
            Language = preferences.Language,
            TimeZone = preferences.TimeZone,
            CreatedDate = preferences.CreatedDate
        };
    }

    public async Task<ResponseResult> UpdatePreferencesAsync(UserPreferencesDto dto)
    {
        try
        {
            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            var preferences = await context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

            if (preferences == null)
            {
                preferences = UserPreferences.CreateDefault(currentUser.Id);
                context.UserPreferences.Add(preferences);
            }

            preferences.UpdateNotificationSettings(
                dto.EmailNotificationsEnabled,
                dto.SmsNotificationsEnabled,
                false,
            false,
            false,
            false
            );

            preferences.UpdateUiSettings("Default", dto.Language, dto.TimeZone);

            await context.SaveChangesAsync();

            // Audit
            await auditService.LogAsync(
                tenantId: currentUser.TenantId,
                userId: currentUser.Id,
                action: "PreferencesUpdated",
                entityName: nameof(UserPreferences),
                severity: AuditSeverity.Info
            );

            logger.LogInformation("User {UserId} updated preferences", currentUser.Id);

            return ResponseResult.Success("Preferences updated successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating preferences");
            return ResponseResult.Failure("An error occurred while updating preferences");
        }
    }
}