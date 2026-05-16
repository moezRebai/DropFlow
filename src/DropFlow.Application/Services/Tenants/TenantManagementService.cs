using DropFlow.Application.Common;
using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Entities;
using DropFlow.Domain.Enums;
using DropFlow.Shared.Common;
using DropFlow.Shared.Tenants;
using DropFlow.Shared.Tenants.Depots;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Application.Services.Tenants;

public class TenantManagementService(
    IApplicationDbContext context,
    ITenantService tenantService,
    IAuditService auditService,
    IAppCacheService cache,
    ILogger<TenantManagementService> logger)
    : ITenantManagementService
{
    private void InvalidateDepotCache() =>
        cache.Remove(CacheKeys.Depots(tenantService.GetTenantId()));

    // ═══════════════════════════════════════════════════════════
    // TENANT INFO
    // ═══════════════════════════════════════════════════════════
    
    public async Task<ResponseResult<TenantDto>> GetCurrentTenantAsync()
    {
        try
        {
            var tenantId = tenantService.GetTenantId();
            
            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);
            
            if (tenant == null)
                return ResponseResult<TenantDto>.Failure("Tenant non trouvé");
            
            var dto = MapTenantToDto(tenant);
            
            return ResponseResult<TenantDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération du tenant");
            return ResponseResult<TenantDto>.Failure("Erreur lors de la récupération des informations");
        }
    }
    
    public async Task<ResponseResult> UpdateCompanyInfoAsync(UpdateTenantCompanyInfoDto dto)
    {
        try
        {
            var tenantId = tenantService.GetTenantId();
            var currentUser = tenantService.GetCurrentUser();
            
            if (currentUser == null)
                return ResponseResult.Failure("Utilisateur non trouvé");
            
            var tenant = await context.Tenants.FindAsync(tenantId);
            
            if (tenant == null)
                return ResponseResult.Failure("Tenant non trouvé");
            
            var oldValues = new
            {
                tenant.CompanyName,
                tenant.Address,
                tenant.City,
                tenant.Phone,
                tenant.Email
            };
            
            // Utiliser la méthode de l'entité pour garantir la cohérence
            tenant.UpdateCompanyInfo(
                companyName: dto.CompanyName,
                address: dto.Address,
                zipCode: dto.ZipCode,
                city: dto.City,
                phone: dto.Phone,
                email: dto.Email,
                website: dto.Website
            );
            
            await context.SaveChangesAsync();
            
            // Audit
            await auditService.LogAsync(
                tenantId: tenantId,
                userId: currentUser.Id,
                action: "TenantCompanyInfoUpdated",
                entityName: nameof(Tenant),
                entityId: tenant.Id,
                changes: new
                {
                    Old = oldValues,
                    New = new
                    {
                        dto.CompanyName,
                        dto.Address,
                        dto.City,
                        dto.Phone,
                        dto.Email
                    }
                },
                severity: AuditSeverity.Info
            );
            
            logger.LogInformation("Informations entreprise mises à jour par {UserId}", currentUser.Id);
            
            return ResponseResult.Success("Informations mises à jour avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la mise à jour des informations entreprise");
            return ResponseResult.Failure("Erreur lors de la mise à jour");
        }
    }
    
    public async Task<ResponseResult> UpdateLegalInfoAsync(UpdateTenantLegalInfoDto dto)
    {
        try
        {
            var tenantId = tenantService.GetTenantId();
            var currentUser = tenantService.GetCurrentUser();
            
            if (currentUser == null)
                return ResponseResult.Failure("Utilisateur non trouvé");
            
            var tenant = await context.Tenants.FindAsync(tenantId);
            
            if (tenant == null)
                return ResponseResult.Failure("Tenant non trouvé");
            
            var oldValues = new
            {
                tenant.Siret,
                tenant.VatNumber,
                tenant.LegalForm
            };
            
            tenant.UpdateLegalInfo(
                siret: dto.Siret,
                vatNumber: dto.VatNumber,
                legalForm: dto.LegalForm,
                legalMentions: dto.LegalMentions,
                bankDetails: dto.BankDetails
            );
            
            await context.SaveChangesAsync();
            
            // Audit
            await auditService.LogAsync(
                tenantId: tenantId,
                userId: currentUser.Id,
                action: "TenantLegalInfoUpdated",
                entityName: nameof(Tenant),
                entityId: tenant.Id,
                changes: new
                {
                    Old = oldValues,
                    New = new { dto.Siret, dto.VatNumber, dto.LegalForm }
                },
                severity: AuditSeverity.Info
            );
            
            logger.LogInformation("Informations légales mises à jour par {UserId}", currentUser.Id);
            
            return ResponseResult.Success("Informations légales mises à jour avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la mise à jour des informations légales");
            return ResponseResult.Failure("Erreur lors de la mise à jour");
        }
    }
    
    public async Task<ResponseResult> UpdateLogoAsync(UpdateTenantLogoDto dto)
    {
        try
        {
            var tenantId = tenantService.GetTenantId();
            var currentUser = tenantService.GetCurrentUser();
            
            if (currentUser == null)
                return ResponseResult.Failure("Utilisateur non trouvé");
            
            var tenant = await context.Tenants.FindAsync(tenantId);
            
            if (tenant == null)
                return ResponseResult.Failure("Tenant non trouvé");
            
            tenant.UpdateLogo(dto.LogoUrl);
            
            await context.SaveChangesAsync();
            
            // Audit
            await auditService.LogAsync(
                tenantId: tenantId,
                userId: currentUser.Id,
                action: "TenantLogoUpdated",
                entityName: nameof(Tenant),
                entityId: tenant.Id,
                changes: new { dto.LogoUrl },
                severity: AuditSeverity.Info
            );
            
            logger.LogInformation("Logo mis à jour par {UserId}", currentUser.Id);
            
            return ResponseResult.Success("Logo mis à jour avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la mise à jour du logo");
            return ResponseResult.Failure("Erreur lors de la mise à jour");
        }
    }
    
    public async Task<ResponseResult> RemoveLogoAsync()
    {
        try
        {
            var tenantId = tenantService.GetTenantId();
            var currentUser = tenantService.GetCurrentUser();
            
            if (currentUser == null)
                return ResponseResult.Failure("Utilisateur non trouvé");
            
            var tenant = await context.Tenants.FindAsync(tenantId);
            
            if (tenant == null)
                return ResponseResult.Failure("Tenant non trouvé");
            
            tenant.RemoveLogo();
            
            await context.SaveChangesAsync();
            
            // Audit
            await auditService.LogAsync(
                tenantId: tenantId,
                userId: currentUser.Id,
                action: "TenantLogoRemoved",
                entityName: nameof(Tenant),
                entityId: tenant.Id,
                changes: new { Action = "Logo supprimé" },
                severity: AuditSeverity.Info
            );
            
            logger.LogInformation("Logo supprimé par {UserId}", currentUser.Id);
            
            return ResponseResult.Success("Logo supprimé avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la suppression du logo");
            return ResponseResult.Failure("Erreur lors de la suppression");
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // DEPOTS
    // ═══════════════════════════════════════════════════════════
    
    public Task<List<TenantDepotDto>> GetAllDepotsAsync()
    {
        var tenantId = tenantService.GetTenantId();
        return cache.GetOrSetAsync(
            CacheKeys.Depots(tenantId),
            FetchAllDepotsAsync,
            TimeSpan.FromHours(24));
    }

    private async Task<List<TenantDepotDto>> FetchAllDepotsAsync()
    {
        try
        {
            return await context.TenantDepots
                .Where(d => d.IsActive)
                .OrderByDescending(d => d.IsDefault)
                .ThenBy(d => d.Name)
                .Select(d => new TenantDepotDto
                {
                    Id = d.Id,
                    TenantId = d.TenantId,
                    Name = d.Name,
                    FullAddress = d.FullAddress,
                    City = d.City,
                    ZipCode = d.ZipCode,
                    Latitude = d.Latitude,
                    Longitude = d.Longitude,
                    IsDefault = d.IsDefault,
                    IsActive = d.IsActive,
                    CreatedDate = d.CreatedDate,
                    ModifiedDate = d.ModifiedDate
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des dépôts");
            return [];
        }
    }
    
    public async Task<PagedResult<TenantDepotDto>> GetDepotsAsync(TenantDepotFilterDto filter)
    {
        try
        {
            var query = context.TenantDepots.AsQueryable();
            
            // Filtres
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(d =>
                    d.Name.Contains(filter.SearchTerm) ||
                    d.FullAddress.Contains(filter.SearchTerm) ||
                    (d.City != null && d.City.Contains(filter.SearchTerm)));
            }
            
            if (filter.IsActive.HasValue)
            {
                query = query.Where(d => d.IsActive == filter.IsActive.Value);
            }
            
            if (filter.IsDefault.HasValue)
            {
                query = query.Where(d => d.IsDefault == filter.IsDefault.Value);
            }
            
            if (!string.IsNullOrEmpty(filter.City))
            {
                query = query.Where(d => d.City == filter.City);
            }
            
            if (!string.IsNullOrEmpty(filter.ZipCode))
            {
                query = query.Where(d => d.ZipCode == filter.ZipCode);
            }
            
            var totalCount = await query.CountAsync();
            
            // Tri : dépôt par défaut en premier, puis par nom
            query = query
                .OrderByDescending(d => d.IsDefault)
                .ThenBy(d => d.Name);
            
            // Pagination
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(d => new TenantDepotDto
                {
                    Id = d.Id,
                    TenantId = d.TenantId,
                    Name = d.Name,
                    FullAddress = d.FullAddress,
                    City = d.City,
                    ZipCode = d.ZipCode,
                    Latitude = d.Latitude,
                    Longitude = d.Longitude,
                    IsDefault = d.IsDefault,
                    IsActive = d.IsActive,
                    CreatedDate = d.CreatedDate,
                    ModifiedDate = d.ModifiedDate
                })
                .ToListAsync();
            
            return new PagedResult<TenantDepotDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des dépôts avec pagination");
            return new PagedResult<TenantDepotDto>
            {
                Items = [],
                TotalCount = 0,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
    }
    
    public async Task<ResponseResult<TenantDepotDto>> GetDepotByIdAsync(int id)
    {
        try
        {
            var depot = await context.TenantDepots
                .FirstOrDefaultAsync(d => d.Id == id);
            
            if (depot == null)
                return ResponseResult<TenantDepotDto>.Failure("Dépôt non trouvé");
            
            var dto = MapDepotToDto(depot);
            
            return ResponseResult<TenantDepotDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération du dépôt {Id}", id);
            return ResponseResult<TenantDepotDto>.Failure("Erreur lors de la récupération");
        }
    }
    
    public async Task<ResponseResult<int>> CreateDepotAsync(CreateTenantDepotDto dto)
    {
        try
        {
            var tenantId = tenantService.GetTenantId();
            var currentUser = tenantService.GetCurrentUser();
            
            if (currentUser == null)
                return ResponseResult<int>.Failure("Utilisateur non trouvé");
            
            // Vérifier unicité du nom par tenant
            var nameExists = await context.TenantDepots
                .AnyAsync(d => d.Name == dto.Name);
            
            if (nameExists)
                return ResponseResult<int>.Failure($"Un dépôt avec le nom '{dto.Name}' existe déjà");
            
            // Si c'est le premier dépôt, le mettre par défaut automatiquement
            var hasExistingDepots = await context.TenantDepots.AnyAsync();
            var isDefault = dto.IsDefault || !hasExistingDepots;
            
            // Si on veut le mettre par défaut, retirer le flag des autres
            if (isDefault)
            {
                var currentDefault = await context.TenantDepots
                    .FirstOrDefaultAsync(d => d.IsDefault);
                
                if (currentDefault != null)
                {
                    currentDefault.UnsetAsDefault();
                }
            }
            
            var depot = TenantDepot.Create(
                tenantId: tenantId,
                name: dto.Name,
                fullAddress: dto.FullAddress,
                city: dto.City,
                zipCode: dto.ZipCode,
                latitude: dto.Latitude,
                longitude: dto.Longitude,
                isDefault: isDefault
            );
            
            if (!dto.IsActive)
            {
                depot.Deactivate();
            }
            
            context.TenantDepots.Add(depot);
            await context.SaveChangesAsync();
            InvalidateDepotCache();

            // Audit
            await auditService.LogAsync(
                tenantId: tenantId,
                userId: currentUser.Id,
                action: "DepotCreated",
                entityName: nameof(TenantDepot),
                entityId: depot.Id,
                changes: new { depot.Name, depot.City, IsDefault = isDefault },
                severity: AuditSeverity.Info
            );
            
            logger.LogInformation("Dépôt {DepotName} créé par {UserId}", depot.Name, currentUser.Id);
            
            return ResponseResult<int>.Success(depot.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la création du dépôt");
            return ResponseResult<int>.Failure("Erreur lors de la création du dépôt");
        }
    }
    
    public async Task<ResponseResult> UpdateDepotAsync(int id, UpdateTenantDepotDto dto)
    {
        try
        {
            var depot = await context.TenantDepots.FindAsync(id);
            
            if (depot == null)
                return ResponseResult.Failure("Dépôt non trouvé");
            
            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("Utilisateur non trouvé");
            
            // Vérifier unicité du nom
            var nameExists = await context.TenantDepots
                .AnyAsync(d => d.Name == dto.Name && d.Id != id);
            
            if (nameExists)
                return ResponseResult.Failure($"Un dépôt avec le nom '{dto.Name}' existe déjà");
            
            var oldValues = new
            {
                depot.Name,
                depot.FullAddress,
                depot.City
            };
            
            depot.Update(
                name: dto.Name,
                fullAddress: dto.FullAddress,
                city: dto.City,
                zipCode: dto.ZipCode,
                latitude: dto.Latitude,
                longitude: dto.Longitude
            );
            
            await context.SaveChangesAsync();
            InvalidateDepotCache();

            // Audit
            await auditService.LogAsync(
                tenantId: depot.TenantId,
                userId: currentUser.Id,
                action: "DepotUpdated",
                entityName: nameof(TenantDepot),
                entityId: depot.Id,
                changes: new
                {
                    Old = oldValues,
                    New = new { dto.Name, dto.FullAddress, dto.City }
                },
                severity: AuditSeverity.Info
            );
            
            logger.LogInformation("Dépôt {DepotName} mis à jour par {UserId}", depot.Name, currentUser.Id);
            
            return ResponseResult.Success("Dépôt mis à jour avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la mise à jour du dépôt {Id}", id);
            return ResponseResult.Failure("Erreur lors de la mise à jour");
        }
    }
    
    public async Task<ResponseResult> DeleteDepotAsync(int id)
    {
        try
        {
            var depot = await context.TenantDepots.FindAsync(id);
            
            if (depot == null)
                return ResponseResult.Failure("Dépôt non trouvé");
            
            // Empêcher la suppression du dépôt par défaut s'il y en a d'autres
            if (depot.IsDefault)
            {
                var otherDepots = await context.TenantDepots
                    .Where(d => d.Id != id)
                    .AnyAsync();
                
                if (otherDepots)
                {
                    return ResponseResult.Failure(
                        "Impossible de supprimer le dépôt par défaut. " +
                        "Veuillez d'abord définir un autre dépôt comme par défaut.");
                }
            }
            
            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("Utilisateur non trouvé");
            
            var depotName = depot.Name;
            
            context.TenantDepots.Remove(depot);
            await context.SaveChangesAsync();
            InvalidateDepotCache();

            // Audit
            await auditService.LogAsync(
                tenantId: depot.TenantId,
                userId: currentUser.Id,
                action: "DepotDeleted",
                entityName: nameof(TenantDepot),
                entityId: id,
                changes: new { DepotName = depotName },
                severity: AuditSeverity.Warning
            );
            
            logger.LogInformation("Dépôt {DepotName} supprimé par {UserId}", depotName, currentUser.Id);
            
            return ResponseResult.Success("Dépôt supprimé avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la suppression du dépôt {Id}", id);
            return ResponseResult.Failure("Erreur lors de la suppression");
        }
    }
    
    public async Task<ResponseResult> SetDefaultDepotAsync(int id)
    {
        try
        {
            var depot = await context.TenantDepots.FindAsync(id);
            
            if (depot == null)
                return ResponseResult.Failure("Dépôt non trouvé");
            
            if (depot.IsDefault)
                return ResponseResult.Failure("Ce dépôt est déjà le dépôt par défaut");
            
            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("Utilisateur non trouvé");
            
            // Retirer le flag par défaut de l'ancien dépôt
            var currentDefault = await context.TenantDepots
                .FirstOrDefaultAsync(d => d.IsDefault && d.Id != id);
            
            if (currentDefault != null)
            {
                currentDefault.UnsetAsDefault();
            }
            
            // Définir le nouveau dépôt par défaut
            depot.SetAsDefault();

            await context.SaveChangesAsync();
            InvalidateDepotCache();

            // Audit
            await auditService.LogAsync(
                tenantId: depot.TenantId,
                userId: currentUser.Id,
                action: "DepotSetAsDefault",
                entityName: nameof(TenantDepot),
                entityId: depot.Id,
                changes: new { depot.Name },
                severity: AuditSeverity.Info
            );
            
            logger.LogInformation("Dépôt {DepotName} défini comme par défaut par {UserId}", depot.Name, currentUser.Id);
            
            return ResponseResult.Success("Dépôt défini comme par défaut avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la définition du dépôt par défaut {Id}", id);
            return ResponseResult.Failure("Erreur lors de l'opération");
        }
    }
    
    public async Task<ResponseResult> ToggleDepotStatusAsync(int id)
    {
        try
        {
            var depot = await context.TenantDepots.FindAsync(id);
            
            if (depot == null)
                return ResponseResult.Failure("Dépôt non trouvé");
            
            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("Utilisateur non trouvé");
            
            var action = depot.IsActive ? "désactivé" : "activé";
            
            if (depot.IsActive)
                depot.Deactivate();
            else
                depot.Activate();
            
            await context.SaveChangesAsync();
            InvalidateDepotCache();

            // Audit
            await auditService.LogAsync(
                tenantId: depot.TenantId,
                userId: currentUser.Id,
                action: depot.IsActive ? "DepotActivated" : "DepotDeactivated",
                entityName: nameof(TenantDepot),
                entityId: depot.Id,
                changes: new { depot.Name, NewStatus = depot.IsActive },
                severity: AuditSeverity.Info
            );
            
            logger.LogInformation("Dépôt {DepotName} {Action} par {UserId}", depot.Name, action, currentUser.Id);
            
            return ResponseResult.Success($"Dépôt {action} avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors du changement de statut du dépôt {Id}", id);
            return ResponseResult.Failure("Erreur lors de l'opération");
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // MAPPING HELPERS
    // ═══════════════════════════════════════════════════════════
    
    private static TenantDto MapTenantToDto(Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            SubDomain = tenant.SubDomain,
            PlanType = tenant.PlanType,
            IsActive = tenant.IsActive,
            CompanyName = tenant.CompanyName,
            LogoUrl = tenant.LogoUrl,
            Address = tenant.Address,
            ZipCode = tenant.ZipCode,
            City = tenant.City,
            Phone = tenant.Phone,
            Email = tenant.Email,
            Website = tenant.Website,
            Siret = tenant.Siret,
            VatNumber = tenant.VatNumber,
            LegalForm = tenant.LegalForm,
            LegalMentions = tenant.LegalMentions,
            BankDetails = tenant.BankDetails,
            CreatedDate = tenant.CreatedDate,
            ModifiedDate = tenant.ModifiedDate
        };
    }
    
    private static TenantDepotDto MapDepotToDto(TenantDepot depot)
    {
        return new TenantDepotDto
        {
            Id = depot.Id,
            TenantId = depot.TenantId,
            Name = depot.Name,
            FullAddress = depot.FullAddress,
            City = depot.City,
            ZipCode = depot.ZipCode,
            Latitude = depot.Latitude,
            Longitude = depot.Longitude,
            IsDefault = depot.IsDefault,
            IsActive = depot.IsActive,
            CreatedDate = depot.CreatedDate,
            ModifiedDate = depot.ModifiedDate
        };
    }
}