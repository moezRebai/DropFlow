using DropFlow.Application.Common;
using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Entities;
using DropFlow.Shared.Enums;
using DropFlow.Shared.Common;
using DropFlow.Shared.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Application.Services.Vehicles;

public class VehicleService(
    IApplicationDbContext context,
    ITenantService tenantService,
    IAppCacheService cache,
    ILogger<VehicleService> logger)
    : IVehicleService
{
    private void InvalidateVehicleCache(DateTime? date = null)
    {
        var tenantId = tenantService.GetTenantId();
        if (date.HasValue)
            cache.Remove(CacheKeys.AvailableVehicles(tenantId, date.Value));
        else
            cache.Remove(CacheKeys.AvailableVehicles(tenantId, DateTime.UtcNow.Date));
    }

    public async Task<PagedResult<VehicleDto>> GetAllAsync(VehicleFilterDto filter)
    {
        var query = context.Vehicles.AsQueryable();

        // Filters
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.ToLower();
            query = query.Where(v =>
                v.Brand.ToLower().Contains(search) ||
                v.Model.ToLower().Contains(search) ||
                v.PlateNumber.ToLower().Contains(search));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(v => v.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(v => v.Brand)
            .ThenBy(v => v.Model)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(v => new VehicleDto
            {
                Id = v.Id,
                Brand = v.Brand,
                Model = v.Model,
                PlateNumber = v.PlateNumber,
                MaxDeliveries = v.MaxDeliveries,
                MaxVolume = v.MaxVolume,
                IsActive = v.IsActive,
                CreatedDate = v.CreatedDate
            })
            .ToListAsync();

        return new PagedResult<VehicleDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }

    public async Task<ResponseResult<VehicleDto>> GetByIdAsync(int id)
    {
        var vehicle = await context.Vehicles
            .Where(v => v.Id == id)
            .Select(v => new VehicleDto
            {
                Id = v.Id,
                Brand = v.Brand,
                Model = v.Model,
                PlateNumber = v.PlateNumber,
                MaxDeliveries = v.MaxDeliveries,
                MaxVolume = v.MaxVolume,
                IsActive = v.IsActive,
                CreatedDate = v.CreatedDate
            })
            .FirstOrDefaultAsync();

        if (vehicle == null)
            return ResponseResult<VehicleDto>.Failure("Vehicle not found");

        return ResponseResult<VehicleDto>.Success(vehicle);
    }

    public async Task<ResponseResult<int>> CreateAsync(CreateVehicleDto dto)
    {
        try
        {
            // Validate plate number uniqueness
            var exists = await context.Vehicles
                .AnyAsync(v => v.PlateNumber == dto.PlateNumber);

            if (exists)
                return ResponseResult<int>.Failure("A vehicle with this plate number already exists");

            var vehicle = Vehicle.Create(
                brand: dto.Brand,
                model: dto.Model,
                plateNumber: dto.PlateNumber,
                maxDeliveries: dto.MaxDeliveries,
                maxVolume: dto.MaxVolume
            );

            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();
            InvalidateVehicleCache();

            logger.LogInformation("Vehicle created: {VehicleId}", vehicle.Id);

            return ResponseResult<int>.Success(vehicle.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating vehicle");
            return ResponseResult<int>.Failure("Error creating vehicle");
        }
    }

    public async Task<ResponseResult> UpdateAsync(int id, UpdateVehicleDto dto)
    {
        try
        {
            var vehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
                return ResponseResult.Failure("Vehicle not found");

            // Validate plate number uniqueness
            var exists = await context.Vehicles
                .AnyAsync(v => v.PlateNumber == dto.PlateNumber && v.Id != id);

            if (exists)
                return ResponseResult.Failure("A vehicle with this plate number already exists");

            vehicle.Update(
                brand: dto.Brand,
                model: dto.Model,
                plateNumber: dto.PlateNumber,
                maxDeliveries: dto.MaxDeliveries,
                maxVolume: dto.MaxVolume,
                isActive: dto.IsActive
            );

            await context.SaveChangesAsync();
            InvalidateVehicleCache();

            logger.LogInformation("Vehicle updated: {VehicleId}", id);

            return ResponseResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating vehicle {VehicleId}", id);
            return ResponseResult.Failure("Error updating vehicle");
        }
    }

    public async Task<ResponseResult> DeleteAsync(int id)
    {
        try
        {
            var vehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
                return ResponseResult.Failure("Vehicle not found");

            var hasAttachedRouteSheets = await context.Routes
                .AnyAsync(rs => rs.VehicleId == id);
            
            if (hasAttachedRouteSheets)
            {
                // Check if vehicle has active route sheets
                var hasActiveRouteSheets = await context.Routes
                    .AnyAsync(rs => rs.VehicleId == id && 
                                    rs.Status != RouteStatus.Completed &&
                                    rs.Status != RouteStatus.Cancelled);

                if (hasActiveRouteSheets)
                    return ResponseResult.Failure("Cannot delete vehicle with active route sheets");

                vehicle.Deactivate();
                await context.SaveChangesAsync();
                InvalidateVehicleCache();

                logger.LogInformation("Vehicle deactivated: {VehicleId}", id);
                return ResponseResult.Success("Vehicle deactivated successfully");
            }

            context.Vehicles.Remove(vehicle);
            await context.SaveChangesAsync();
            InvalidateVehicleCache();

            logger.LogInformation("Vehicle deleted: {VehicleId}", id);
            return ResponseResult.Success("Vehicle deleted successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting vehicle {VehicleId}", id);
            return ResponseResult.Failure("Error deleting vehicle");
        }
    }

    public async Task<ResponseResult<bool>> IsAvailableAsync(int vehicleId, DateTime date)
    {
        var hasRouteSheet = await context.Routes
            .AnyAsync(rs =>
                rs.VehicleId == vehicleId &&
                rs.Date.Date == date.Date &&
                rs.Status != RouteStatus.Cancelled);

        return ResponseResult<bool>.Success(!hasRouteSheet);
    }

    public Task<List<VehicleDto>> GetAvailableVehiclesAsync(DateTime date)
    {
        var tenantId = tenantService.GetTenantId();
        return cache.GetOrSetAsync(
            CacheKeys.AvailableVehicles(tenantId, date),
            () => FetchAvailableVehiclesAsync(date),
            TimeSpan.FromMinutes(10));
    }

    private async Task<List<VehicleDto>> FetchAvailableVehiclesAsync(DateTime date)
    {
        var busyStatus = new List<RouteStatus>
        {
            RouteStatus.Confirmed,
            RouteStatus.InProgress
        };

        var busyVehicleIds = await context.Routes
            .Where(rs => rs.Date.Date == date.Date && busyStatus.Contains(rs.Status))
            .Select(rs => rs.VehicleId)
            .ToListAsync();

        return await context.Vehicles
            .Where(v => v.IsActive && !busyVehicleIds.Contains(v.Id))
            .Select(v => new VehicleDto
            {
                Id = v.Id,
                Brand = v.Brand,
                Model = v.Model,
                PlateNumber = v.PlateNumber,
                MaxDeliveries = v.MaxDeliveries,
                MaxVolume = v.MaxVolume,
                IsActive = v.IsActive,
                CreatedDate = v.CreatedDate
            })
            .ToListAsync();
    }
}