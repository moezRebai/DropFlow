using DropFlow.Application.Common;
using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Entities;
using DropFlow.Shared.Common;
using DropFlow.Shared.Drivers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Exception = System.Exception;

namespace DropFlow.Application.Services.Drivers;

public class DriverService(
    IApplicationDbContext context,
    ITenantService tenantService,
    IAppCacheService cache,
    ILogger<DriverService> logger)
    : IDriverService
{
    private void InvalidateDriverCache() =>
        cache.Remove(CacheKeys.ActiveDrivers(tenantService.GetTenantId()));

    public async Task<PagedResult<DriverDto>> GetAllAsync(DriverFilterDto filter)
    {
        var query = context.Drivers
            .Include(d => d.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.ToLower();
            query = query.Where(d =>
                d.User.FirstName.ToLower().Contains(search) ||
                d.User.LastName.ToLower().Contains(search) ||
                d.User.Email!.ToLower().Contains(search) ||
                (d.LicenseNumber != null && d.LicenseNumber.ToLower().Contains(search)));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(d => d.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(d => d.User.FirstName)
            .ThenBy(d => d.User.LastName)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(d => new DriverDto
            {
                Id = d.Id,
                UserId = d.UserId,
                FirstName = d.User.FirstName,
                LastName = d.User.LastName,
                Email = d.User.Email!,
                Phone = d.User.PhoneNumber ?? "",
                LicenseNumber = d.LicenseNumber,
                LicenseExpiryDate = d.LicenseExpiryDate,
                IsActive = d.IsActive,
                CreatedDate = d.CreatedDate
            })
            .ToListAsync();

        return new PagedResult<DriverDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }

    public async Task<ResponseResult<DriverDto>> GetByIdAsync(int id)
    {
        var driver = await context.Drivers
            .Include(d => d.User)
            .Where(d => d.Id == id)
            .Select(d => new DriverDto
            {
                Id = d.Id,
                UserId = d.UserId,
                FirstName = d.User.FirstName,
                LastName = d.User.LastName,
                Email = d.User.Email!,
                Phone = d.User.PhoneNumber ?? "",
                LicenseNumber = d.LicenseNumber,
                LicenseExpiryDate = d.LicenseExpiryDate,
                IsActive = d.IsActive,
                CreatedDate = d.CreatedDate
            })
            .FirstOrDefaultAsync();

        return driver == null ? ResponseResult<DriverDto>.Failure("Driver not found") : ResponseResult<DriverDto>.Success(driver);
    }

    public async Task<ResponseResult<DriverDto>> GetByUserIdAsync(string userId)
    {
        var driver = await context.Drivers
            .Include(d => d.User)
            .Where(d => d.UserId == userId)
            .Select(d => new DriverDto
            {
                Id = d.Id,
                UserId = d.UserId,
                FirstName = d.User.FirstName,
                LastName = d.User.LastName,
                Email = d.User.Email!,
                Phone = d.User.PhoneNumber ?? "",
                LicenseNumber = d.LicenseNumber,
                LicenseExpiryDate = d.LicenseExpiryDate,
                IsActive = d.IsActive,
                CreatedDate = d.CreatedDate
            })
            .FirstOrDefaultAsync();

        return driver == null ? ResponseResult<DriverDto>.Failure("Driver not found") : ResponseResult<DriverDto>.Success(driver);
    }

    public async Task<ResponseResult<int>> CreateAsync(CreateDriverDto dto)
    {
        try
        {
            // Validate user exists
            var userExists = await context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
                return ResponseResult<int>.Failure("Utilisateur introuvable");

            // Validate user not already a driver
            var isDriver = await context.Drivers.AnyAsync(d => d.UserId == dto.UserId);
            if (isDriver)
                return ResponseResult<int>.Failure("Cet utilisateur est déjà un livreur");

            var driver = Driver.Create(
                userId: dto.UserId,
                licenseNumber: dto.LicenseNumber,
                licenseExpiryDate: dto.LicenseExpiryDate);

            context.Drivers.Add(driver);
            await context.SaveChangesAsync();
            InvalidateDriverCache();

            logger.LogInformation("Driver created: {DriverId}", driver.Id);

            return ResponseResult<int>.Success(driver.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating driver");
            return ResponseResult<int>.Failure("Error creating driver");
        }
    }

    public async Task<ResponseResult> UpdateAsync(int id, UpdateDriverDto dto)
    {
        try
        {
            var driver = await context.Drivers.FindAsync(id);

            if (driver == null)
                return ResponseResult.Failure("Driver not found");

            driver.Update(
                licenseNumber: dto.LicenseNumber,
                licenseExpiryDate: dto.LicenseExpiryDate);

            await context.SaveChangesAsync();
            InvalidateDriverCache();

            logger.LogInformation("Driver updated: {DriverId}", id);

            return ResponseResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating driver {DriverId}", id);
            return ResponseResult.Failure("Error updating driver");
        }
    }

    public async Task<ResponseResult> DeleteAsync(int id)
    {
        try
        {
            var driver = await context.Drivers.FindAsync(id);

            if (driver == null)
                return ResponseResult.Failure("Driver not found");

            // Check active route s
            var hasActiveRoutes = await context.RouteTeams
                .Include(rsc => rsc.Route)
                .AnyAsync(rsc => rsc.DriverId == id &&
                    rsc.Route.Status != Domain.Enums.RouteStatus.Completed &&
                    rsc.Route.Status != Domain.Enums.RouteStatus.Cancelled);

            if (hasActiveRoutes)
                return ResponseResult.Failure("Impossible de supprimer un livreur avec des tournées actives");

            driver.Deactivate();
            await context.SaveChangesAsync();
            InvalidateDriverCache();

            logger.LogInformation("Driver deactivated: {DriverId}", id);

            return ResponseResult.Success("Livreur désactivé avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting driver {DriverId}", id);
            return ResponseResult.Failure("Error deleting driver");
        }
    }

    public Task<List<DriverDto>> GetActiveDriversAsync()
    {
        var tenantId = tenantService.GetTenantId();
        return cache.GetOrSetAsync(
            CacheKeys.ActiveDrivers(tenantId),
            FetchActiveDriversAsync,
            TimeSpan.FromHours(4));
    }

    private async Task<List<DriverDto>> FetchActiveDriversAsync()
    {
        return await context.Drivers
            .Include(d => d.User)
            .Where(d => d.IsActive)
            .Select(d => new DriverDto
            {
                Id = d.Id,
                UserId = d.UserId,
                FirstName = d.User.FirstName,
                LastName = d.User.LastName,
                Email = d.User.Email!,
                Phone = d.User.PhoneNumber ?? "",
                LicenseNumber = d.LicenseNumber,
                LicenseExpiryDate = d.LicenseExpiryDate,
                IsActive = d.IsActive,
                CreatedDate = d.CreatedDate
            })
            .OrderBy(d => d.FirstName)
            .ThenBy(d => d.LastName)
            .ToListAsync();
    }
}