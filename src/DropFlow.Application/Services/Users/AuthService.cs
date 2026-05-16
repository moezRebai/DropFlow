using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Emails;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Constants;
using DropFlow.Domain.Entities;
using DropFlow.Domain.Enums;
using DropFlow.Shared.Auth;
using DropFlow.Shared.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DropFlow.Application.Services.Users;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    IApplicationDbContext context,
    IConfiguration configuration,
    IAuditService auditService,
    IEmailService emailService,
    ITokenBlacklistService tokenBlacklist,
    ILogger<AuthService> logger)
    : IAuthService
{
    public async Task<List<UserTenantInfoDto>> GetUserTenantsAsync(string email)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(email))
                return [];
        
            // ✅ NOUVEAU : Chercher TOUS les users avec cet email (multi-tenant)
            var users = await context.Users
                .Where(u => u.Email == email)
                .ToListAsync();
        
            if (!users.Any())
                return [];
        
            var tenantInfos = new List<UserTenantInfoDto>();
        
            foreach (var user in users)
            {
                // Récupérer le tenant
                if (user.TenantId == TenantIds.DropFlowAdmin)
                {
                    tenantInfos.Add(new UserTenantInfoDto(
                        user.TenantId,
                        "DropFlow Admin",
                        Roles.Admin,
                        user.IsActive
                    ));
                    break;
                }
                
                var tenant = await context.Tenants.FindAsync(user.TenantId);
                if (tenant == null) continue;
            
                // Récupérer le rôle
                var roles = await userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User";
            
                tenantInfos.Add(new UserTenantInfoDto(
                    user.TenantId,
                    tenant.Name,
                    role,
                    user.IsActive
                ));
            }
        
            // Retourner seulement les tenants actifs
            return tenantInfos.Where(t => t.IsActive).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user tenants for email {Email}", email);
            return [];
        }
    }
    public async Task<AuthResult> RegisterTenantAsync(RegisterTenantDto dto)
    {
        try
        {
            if (dto.Password != dto.ConfirmPassword)
                return new AuthResult(false, Message: "Les mots de passe ne correspondent pas");

            var normalizedCompanyName = dto.CompanyName.Trim().ToUpperInvariant();
            var existingTenant = await context.Tenants
                .Where(t => t.Name == normalizedCompanyName && t.IsActive)
                .FirstOrDefaultAsync();

            if (existingTenant != null)
                return new AuthResult(false, Message: "Ce nom d'entreprise est déjà utilisé");


            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // 1. Créer le Tenant
                var tenant = Tenant.Create(dto.CompanyName);
                context.Tenants.Add(tenant);
                await context.SaveChangesAsync();

                // 2. Créer l'utilisateur Manager
                var user = ApplicationUser.Create(
                    tenant.Id,
                    dto.Email,
                    dto.FirstName,
                    dto.LastName);

                var result = await userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return new AuthResult(false,
                        Message: string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                // 3. Assigner le rôle Manager
                await userManager.AddToRoleAsync(user, Roles.Manager);

                // 4. Audit log
                await auditService.LogAsync(
                    tenantId: tenant.Id,
                    userId: user.Id,
                    action: AuditActions.TenantCreated,
                    entityName: nameof(Tenant),
                    entityId: tenant.Id,
                    changes: new { CompanyName = tenant.Name },
                    severity: AuditSeverity.Critical);

                await transaction.CommitAsync();

                // 5. Générer JWT + refresh token
                var token = GenerateJwtToken(user, Roles.Manager, tenant);
                var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);
                await context.SaveChangesAsync();

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Phone = user.PhoneNumber!,
                    Address = user.Address!,
                    Role = Roles.Manager,
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    IsActive = user.IsActive
                };

                // 6. Email de bienvenue
                await emailService.SendWelcomeEmailAsync(user.Email!, user.FirstName);

                logger.LogInformation("Tenant created successfully: {TenantId}", tenant.Id);

                return new AuthResult(true, Token: token, RefreshToken: refreshToken, User: userDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Error creating tenant");
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error in RegisterTenantAsync");
            return new AuthResult(false, Message: "Une erreur s'est produite lors de l'inscription");
        }
    }
    public async Task<AuthResult> LoginAsync(LoginDto dto)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email && u.TenantId == dto.TenantId);
        
        if (user == null)
        {
            return new AuthResult(false, Message: "Email ou mot de passe incorrect");
        }

        if (!user.IsActive)
            return new AuthResult(false, Message: "Votre compte est désactivé");

        // ✅ EXCEPTION POUR ADMIN DROPFLOW (TenantId = 0)
        if (user.TenantId != TenantIds.DropFlowAdmin)
        {
            var userTenant = await context.Tenants.FindAsync(user.TenantId);
            if (userTenant is not { IsActive: true })
                return new AuthResult(false, Message: "Votre entreprise est désactivée");
        }

        if (!await userManager.CheckPasswordAsync(user, dto.Password))
        {
            await userManager.AccessFailedAsync(user);
            await auditService.LogAsync(
                tenantId: user.TenantId,
                userId: user.Id,
                action: AuditActions.LoginFailed,
                entityName: nameof(ApplicationUser),
                changes: new { UserId = user.Id, Reason = "Invalid password" },
                severity: AuditSeverity.Warning
            );
            return new AuthResult(false, Message: "Email ou mot de passe incorrect");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        user.UpdateLastLogin();
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? Roles.Livreur;

        // ✅ Pour Admin, pas de Tenant
        Tenant? tenant = null;
        if (user.TenantId != TenantIds.DropFlowAdmin)
        {
            tenant = await context.Tenants.FindAsync(user.TenantId);

            if (tenant == null || !tenant.IsActive)
                return new AuthResult(false, Message: "Votre entreprise est désactivée");
        }
        
        var token = GenerateJwtToken(user, role, tenant);
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);
        await context.SaveChangesAsync();

        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.PhoneNumber!,
            Address = user.Address!,
            Role = role,
            TenantId = user.TenantId,
            TenantName = tenant?.Name ?? "DropFlow Admin",
            IsActive = user.IsActive
        };

        return new AuthResult(true, Token: token, RefreshToken: refreshToken, User: userDto);
    }
    public async Task<AuthResult> AcceptInvitationAsync(AcceptInvitationDto dto)
    {
        try
        {
            if (dto.Password != dto.ConfirmPassword)
                return new AuthResult(false, Message: "Les mots de passe ne correspondent pas");

            var invitation = await context.UserInvitations
                .FirstOrDefaultAsync(i => i.Token == dto.Token && !i.IsUsed);


            if (invitation == null)
                return new AuthResult(false, Message: "Invitation invalide");

            if (!invitation.IsValid())
                return new AuthResult(false, Message: "Cette invitation a expiré");

            var tenant = await context.Tenants.FindAsync(invitation.TenantId);
            if (tenant == null)
                return new AuthResult(false, Message: "Entreprise introuvable");

            var user = ApplicationUser.Create(
                invitation.TenantId,
                invitation.Email,
                dto.FirstName,
                dto.LastName);

            var result = await userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return new AuthResult(false,
                    Message: string.Join(", ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(user, invitation.Role);

            invitation.MarkAsUsed();
            var inviteRefreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);
            await context.SaveChangesAsync();

            await auditService.LogAsync(
                tenantId: invitation.TenantId,
                userId: user.Id,
                action: AuditActions.InvitationAccepted,
                entityName: nameof(ApplicationUser),
                changes: new { UserId = user.Id, invitation.Role },
                severity: AuditSeverity.Info);

            var token = GenerateJwtToken(user, invitation.Role, invitation.Tenant);

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.PhoneNumber!,
                Address = user.Address!,
                Role = invitation.Role,
                TenantId = invitation.TenantId,
                TenantName = invitation.Tenant.Name,
                IsActive = user.IsActive
            };

            return new AuthResult(true, Token: token, RefreshToken: inviteRefreshToken, User: userDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in AcceptInvitationAsync");
            return new AuthResult(false, Message: "Une erreur s'est produite");
        }
    }
    public async Task<ResponseResult> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        try
        {
            // Validation email
            if (string.IsNullOrWhiteSpace(dto.Email))
                return ResponseResult.Failure("Email is required");

            // Chercher l'utilisateur
            var users = await context.Users
                .Where(u => u.Email == dto.Email && u.IsActive)
                .ToListAsync();
            
            // IMPORTANT : Pour des raisons de sécurité, on retourne toujours succès
            if (!users.Any())
            {
                logger.LogWarning(
                    "Password reset requested for non-existent email: {Email}", 
                    dto.Email);
            
                return ResponseResult.Success(
                    "If the email exists, a password reset link has been sent");
            }

            // Vérifier que le compte est actif
            foreach (var user in users)
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            
                var userName = string.IsNullOrWhiteSpace(user.FirstName) 
                    ? user.Email 
                    : $"{user.FirstName} {user.LastName}";

                var tenant = await context.Tenants.FindAsync(user.TenantId);
                var roles = await userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User";

                // ✅ IMPORTANT : Inclure TenantId dans l'email
                await emailService.SendPasswordResetEmailAsync(
                    user.Email!, 
                    resetToken, 
                    userName,
                    tenant?.Name ?? "Unknown",  // ✅ Nom du tenant
                    user.TenantId,              // ✅ TenantId
                    role); 

                // Audit
                await auditService.LogAsync(
                    tenantId: user.TenantId,
                    userId: user.Id,
                    action: "PasswordResetRequested",
                    entityName: nameof(ApplicationUser),
                    severity: AuditSeverity.Info
                );
            }
            
            logger.LogInformation("Password reset emails sent to {Count} accounts for {Email}", users.Count, dto.Email);   
            return ResponseResult.Success("If the email exists, a password reset link has been sent");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing forgot password request");
            return ResponseResult.Failure("An error occurred. Please try again later");
        }
    }
    public async Task<ResponseResult> ResetPasswordAsync(ResetPasswordDto dto)
    {
        try
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Email))
                return ResponseResult.Failure("Email is required");

            if (string.IsNullOrWhiteSpace(dto.Token))
                return ResponseResult.Failure("Reset token is required");

            if (dto.NewPassword != dto.ConfirmNewPassword)
                return ResponseResult.Failure("Passwords do not match");

            var user = await context.Users
                .FirstOrDefaultAsync(u => 
                    u.Email == dto.Email && 
                    u.TenantId == dto.TenantId);
            
            if (user == null)
                return ResponseResult.Failure("Invalid reset token");

            // Vérifier que le compte est actif
            if (!user.IsActive)
                return ResponseResult.Failure("Account is disabled");

            // Réinitialiser le mot de passe avec le token
            var result = await userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));

                logger.LogWarning(
                    "Password reset failed for {Email}: {Errors}",
                    dto.Email, errors);

                // Si le token est invalide ou expiré
                return result.Errors.Any(e => e.Code == "InvalidToken") 
                    ? ResponseResult.Failure("Invalid or expired reset token. Please request a new password reset") 
                    : ResponseResult.Failure(errors);
            }

            // Audit
            await auditService.LogAsync(
                tenantId: user.TenantId,
                userId: user.Id,
                action: "PasswordResetCompleted",
                entityName: nameof(ApplicationUser),
                severity: AuditSeverity.Warning
            );

            logger.LogInformation("Password reset successful for {Email}", dto.Email);

            return ResponseResult.Success("Password reset successful. You can now login with your new password");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting password");
            return ResponseResult.Failure("An error occurred. Please try again later");
        }
    }
    private string GenerateJwtToken(ApplicationUser user, string role, Tenant? tenant)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, role),
            new Claim("TenantId", user.TenantId.ToString()),
            new Claim("IsActive", user.IsActive.ToString()),
            tenant != null
                ? new Claim("TenantName", tenant.Name)
                : new Claim("TenantName", "DropFlow Platform")
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(
                int.Parse(jwtSettings["ExpirationHours"]!)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> GenerateAndStoreRefreshTokenAsync(string userId)
    {
        var tokenBytes = new byte[64];
        RandomNumberGenerator.Fill(tokenBytes);
        var token = Convert.ToBase64String(tokenBytes);

        var jwtSettings = configuration.GetSection("JwtSettings");
        var expirationDays = int.Parse(
            jwtSettings["RefreshTokenExpirationDays"] ?? "30");

        context.RefreshTokens.Add(new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays)
        });

        return token;
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var stored = await context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (stored == null || !stored.IsActive)
                return new AuthResult(false, Message: "Refresh token invalide ou expiré");

            var user = stored.User;
            if (!user.IsActive)
                return new AuthResult(false, Message: "Votre compte est désactivé");

            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? Roles.Livreur;

            Tenant? tenant = null;
            if (user.TenantId != TenantIds.DropFlowAdmin)
            {
                tenant = await context.Tenants.FindAsync(user.TenantId);
                if (tenant == null || !tenant.IsActive)
                    return new AuthResult(false, Message: "Votre entreprise est désactivée");
            }

            stored.IsRevoked = true;
            var newRefreshToken = await GenerateAndStoreRefreshTokenAsync(user.Id);
            await context.SaveChangesAsync();

            var newJwt = GenerateJwtToken(user, role, tenant);

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.PhoneNumber ?? string.Empty,
                Address = user.Address ?? string.Empty,
                Role = role,
                TenantId = user.TenantId,
                TenantName = tenant?.Name ?? "DropFlow Platform",
                IsActive = user.IsActive
            };

            return new AuthResult(true, Token: newJwt, RefreshToken: newRefreshToken, User: userDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in RefreshTokenAsync");
            return new AuthResult(false, Message: "Une erreur s'est produite");
        }
    }

    public async Task RevokeTokenAsync(string jti, string? refreshToken, DateTime jwtExpiry)
    {
        tokenBlacklist.Revoke(jti, jwtExpiry);

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var stored = await context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (stored != null)
            {
                stored.IsRevoked = true;
                await context.SaveChangesAsync();
            }
        }
    }
}