using DropFlow.Application.Common;
using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Entities;
using DropFlow.Shared.Enums;
using DropFlow.Domain.Enums;
using DropFlow.Domain.Maps;
using DropFlow.Shared.Common;
using DropFlow.Shared.Stores;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Application.Services.Stores;

public class StoreService(
    IApplicationDbContext context,
    ITenantService tenantService,
    IAuditService auditService,
    IGeocodingService geocodingService,
    IValidator<CreateStoreDto> createValidator,
    IValidator<UpdateStoreDto> updateValidator,
    IAppCacheService cache,
    ILogger<StoreService> logger)
    : IStoreService
{
    private void InvalidateStoreCache() =>
        cache.Remove(CacheKeys.StoresLookup(tenantService.GetTenantId()));

    public async Task<ResponseResult<int>> CreateStoreAsync(CreateStoreDto dto)
    {
        var validationResult = await createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return ResponseResult<int>.Failure(
                validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        try
        {
            var tenantId = tenantService.GetTenantId();
            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult<int>.Failure("User not found");

            // V�rifier unicit� du nom par tenant
            var nameExists = await context.Stores
                .AnyAsync(s => s.Name == dto.Name);

            if (nameExists)
                return ResponseResult<int>.Failure($"Un magasin avec le nom '{dto.Name}' existe d�j�");

            // G�ocodage si adresse fournie
            GeocodeAddress? geocode = null;
            if (!string.IsNullOrWhiteSpace(dto.Address) && 
                !string.IsNullOrWhiteSpace(dto.ZipCode) && 
                !string.IsNullOrWhiteSpace(dto.City))
            {
                geocode = await geocodingService.GeocodeAddressAsync(
                    dto.Address,
                    dto.ZipCode,
                    dto.City);
            }

            var store = new Store
            {
                TenantId = tenantId,
                Name = dto.Name,
                Address = dto.Address,
                ZipCode = dto.ZipCode,
                City = dto.City,
                Latitude = geocode?.Latitude,
                Longitude = geocode?.Longitude,
                ContactName = dto.ContactName,
                Phone = dto.Phone,
                Email = dto.Email,
                Notes = dto.Notes,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = currentUser.Id
            };

            context.Stores.Add(store);
            await context.SaveChangesAsync();
            InvalidateStoreCache();

            // Audit
            await auditService.LogAsync(
                tenantId: tenantId,
                userId: currentUser.Id,
                action: "StoreCreated",
                entityName: nameof(Store),
                entityId: store.Id,
                changes: new { store.Name, store.City },
                severity: AuditSeverity.Info
            );

            logger.LogInformation("Magasin {StoreName} cr�� par {UserId}", store.Name, currentUser.Id);

            return ResponseResult<int>.Success(store.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la cr�ation du magasin");
            return ResponseResult<int>.Failure("Erreur lors de la cr�ation du magasin");
        }
    }
    public async Task<ResponseResult<StoreDto>> GetStoreByIdAsync(int id)
    {
        try
        {
            var store = await context.Stores
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null)
                return ResponseResult<StoreDto>.Failure("Magasin non trouv�");

            var dto = MapToDto(store);

            return ResponseResult<StoreDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la r�cup�ration du magasin {Id}", id);
            return ResponseResult<StoreDto>.Failure("Erreur lors de la r�cup�ration");
        }
    }
    public async Task<PagedResult<StoreDto>> GetStoresAsync(StoreFilterDto filter)
    {
        try
        {
            var query = context.Stores.AsQueryable();

            // Filtres
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(s =>
                    s.Name.Contains(filter.SearchTerm) ||
                    s.City.Contains(filter.SearchTerm) ||
                    s.ContactName.Contains(filter.SearchTerm));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(s => s.IsActive == filter.IsActive.Value);
            }

            var totalCount = await query.CountAsync();

            // Tri par d�faut : nom
            query = query.OrderBy(s => s.Name);

            // Pagination
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(s => new StoreDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    ZipCode = s.ZipCode,
                    City = s.City,
                    ContactName = s.ContactName,
                    Phone = s.Phone,
                    Email = s.Email,
                    Notes = s.Notes,
                    IsActive = s.IsActive,
                    CreatedDate = s.CreatedDate
                })
                .ToListAsync();

            return new PagedResult<StoreDto>
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
            logger.LogError(ex, "Erreur lors de la r�cup�ration des magasins");
            return new PagedResult<StoreDto>
            {
                Items = [],
                TotalCount = 0,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
    }
    
    public async Task<List<StoreDto>> GetAllStoresAsync()
    {
        try
        {
            var stores = await context.Stores
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new StoreDto()
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    ZipCode = s.ZipCode,
                    City = s.City,
                    ContactName = s.ContactName,
                    Phone = s.Phone,
                    Email = s.Email,
                    Notes = s.Notes,
                    IsActive = s.IsActive,
                    CreatedDate = s.CreatedDate
                })
                .ToListAsync();

            return stores;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la r�cup�ration de la liste des magasins");
            return [];
        }
    }
    
    public Task<List<StoreLookupDto>> GetStoresLookupAsync()
    {
        var tenantId = tenantService.GetTenantId();
        return cache.GetOrSetAsync(
            CacheKeys.StoresLookup(tenantId),
            FetchStoresLookupAsync,
            TimeSpan.FromHours(6));
    }

    private async Task<List<StoreLookupDto>> FetchStoresLookupAsync()
    {
        try
        {
            return await context.Stores
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new StoreLookupDto { Id = s.Id, Name = s.Name, City = s.City })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la r�cup�ration de la liste des magasins");
            return [];
        }
    }
    public async Task<ResponseResult> UpdateStoreAsync(int id, UpdateStoreDto dto)
    {
        var validationResult = await updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return ResponseResult.Failure(
                validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        try
        {
            var store = await context.Stores.FirstOrDefaultAsync(s => s.Id == id);

            if (store == null)
                return ResponseResult.Failure("Magasin non trouv�");

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            // V�rifier unicit� du nom (sauf pour le store actuel)
            var nameExists = await context.Stores
                .AnyAsync(s => s.Name == dto.Name && s.Id != id);

            if (nameExists)
                return ResponseResult.Failure($"Un magasin avec le nom '{dto.Name}' existe d�j�");

            var oldValues = new
            {
                store.Name,
                store.Address,
                store.City,
                store.IsActive
            };

            // G�ocodage si adresse modifi�e
            if (dto.Address != store.Address || 
                dto.ZipCode != store.ZipCode || 
                dto.City != store.City)
            {
                if (!string.IsNullOrWhiteSpace(dto.Address) && 
                    !string.IsNullOrWhiteSpace(dto.ZipCode) && 
                    !string.IsNullOrWhiteSpace(dto.City))
                {
                    var geocode = await geocodingService.GeocodeAddressAsync(
                        dto.Address,
                        dto.ZipCode,
                        dto.City);

                    store.Latitude = geocode.Latitude;
                    store.Longitude = geocode.Longitude;
                }
            }

            // Update properties
            store.Name = dto.Name;
            store.Address = dto.Address;
            store.ZipCode = dto.ZipCode;
            store.City = dto.City;
            store.ContactName = dto.ContactName;
            store.Phone = dto.Phone;
            store.Email = dto.Email;
            store.Notes = dto.Notes;
            store.IsActive = dto.IsActive;
            store.ModifiedDate = DateTime.UtcNow;
            store.ModifiedBy = currentUser.Id;

            await context.SaveChangesAsync();
            InvalidateStoreCache();

            // Audit
            await auditService.LogAsync(
                tenantId: store.TenantId,
                userId: currentUser.Id,
                action: "StoreUpdated",
                entityName: nameof(Store),
                entityId: store.Id,
                changes: new
                {
                    Old = oldValues,
                    New = new { dto.Name, dto.Address, dto.City, dto.IsActive }
                },
                severity: AuditSeverity.Info
            );

            logger.LogInformation("Magasin {StoreName} mis � jour par {UserId}", store.Name, currentUser.Id);

            return ResponseResult.Success("Magasin mis � jour avec succ�s");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la mise � jour du magasin {Id}", id);
            return ResponseResult.Failure("Erreur lors de la mise � jour");
        }
    }
    public async Task<ResponseResult> DeleteStoreAsync(int id)
    {
        try
        {
            var store = await context.Stores.FirstOrDefaultAsync(s => s.Id == id);

            if (store == null)
                return ResponseResult.Failure("Magasin non trouv�");

            // V�rifier si des livraisons utilisent ce magasin
            var hasDeliveries = await context.Deliveries
                .AnyAsync(d => d.StoreId == id);

            if (hasDeliveries)
            {
                return ResponseResult.Failure(
                    "Impossible de supprimer ce magasin car il est utilis� par des livraisons. " +
                    "Vous pouvez le d�sactiver � la place.");
            }

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            var storeName = store.Name;

            context.Stores.Remove(store);
            await context.SaveChangesAsync();
            InvalidateStoreCache();

            // Audit
            await auditService.LogAsync(
                tenantId: store.TenantId,
                userId: currentUser.Id,
                action: "StoreDeleted",
                entityName: nameof(Store),
                entityId: id,
                changes: new { StoreName = storeName },
                severity: AuditSeverity.Warning
            );

            logger.LogInformation("Magasin {StoreName} supprim� par {UserId}", storeName, currentUser.Id);

            return ResponseResult.Success("Magasin supprim� avec succ�s");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la suppression du magasin {Id}", id);
            return ResponseResult.Failure("Erreur lors de la suppression");
        }
    }

    public async Task<ResponseResult> ActivateStoreAsync(int id)
    {
        try
        {
            var store = await context.Stores.FirstOrDefaultAsync(s => s.Id == id);

            if (store == null)
                return ResponseResult.Failure("Magasin non trouv�");

            if (store.IsActive)
                return ResponseResult.Failure("Le magasin est d�j� actif");

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            store.IsActive = true;
            store.ModifiedDate = DateTime.UtcNow;
            store.ModifiedBy = currentUser.Id;

            await context.SaveChangesAsync();
            InvalidateStoreCache();

            // Audit
            await auditService.LogAsync(
                tenantId: store.TenantId,
                userId: currentUser.Id,
                action: "StoreActivated",
                entityName: nameof(Store),
                entityId: store.Id,
                changes: new { store.Name },
                severity: AuditSeverity.Info
            );

            logger.LogInformation("Magasin {StoreName} activ� par {UserId}", store.Name, currentUser.Id);

            return ResponseResult.Success("Magasin activ� avec succ�s");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'activation du magasin {Id}", id);
            return ResponseResult.Failure("Erreur lors de l'activation");
        }
    }

    public async Task<ResponseResult> DeactivateStoreAsync(int id)
    {
        try
        {
            var store = await context.Stores.FirstOrDefaultAsync(s => s.Id == id);

            if (store == null)
                return ResponseResult.Failure("Magasin non trouv�");

            if (!store.IsActive)
                return ResponseResult.Failure("Le magasin est d�j� inactif");

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            store.IsActive = false;
            store.ModifiedDate = DateTime.UtcNow;
            store.ModifiedBy = currentUser.Id;

            await context.SaveChangesAsync();
            InvalidateStoreCache();

            // Audit
            await auditService.LogAsync(
                tenantId: store.TenantId,
                userId: currentUser.Id,
                action: "StoreDeactivated",
                entityName: nameof(Store),
                entityId: store.Id,
                changes: new { store.Name },
                severity: AuditSeverity.Warning
            );

            logger.LogInformation("Magasin {StoreName} d�sactiv� par {UserId}", store.Name, currentUser.Id);

            return ResponseResult.Success("Magasin d�sactiv� avec succ�s");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la d�sactivation du magasin {Id}", id);
            return ResponseResult.Failure("Erreur lors de la d�sactivation");
        }
    }
    private static StoreDto MapToDto(Store store)
    {
        return new StoreDto
        {
            Id = store.Id,
            Name = store.Name,
            Address = store.Address,
            ZipCode = store.ZipCode,
            City = store.City,
            ContactName = store.ContactName,
            Phone = store.Phone,
            Email = store.Email,
            Notes = store.Notes,
            IsActive = store.IsActive,
            CreatedDate = store.CreatedDate
        };
    }
}