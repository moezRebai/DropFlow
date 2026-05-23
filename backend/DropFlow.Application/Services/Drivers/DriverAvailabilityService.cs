using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Shared.Enums;
using DropFlow.Shared.Drivers;
using Microsoft.EntityFrameworkCore;

namespace DropFlow.Application.Services.Drivers;

public class DriverAvailabilityService(IApplicationDbContext context) : IDriverAvailabilityService
{
    public async Task<DriverAvailabilityDto> CheckAvailabilityAsync(int driverId, DateTime date)
    {
        var driver = await context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == driverId);

        if (driver == null)
        {
            return new DriverAvailabilityDto
            {
                DriverId = driverId,
                DriverName = "Inconnu",
                IsAvailable = false,
                ConflictReason = "Livreur introuvable"
            };
        }

        var hasRouteSheet = await context.RouteTeams
            .Include(rsc => rsc.Route)
            .AnyAsync(rsc =>
                rsc.DriverId == driverId &&
                rsc.Route.Date.Date == date.Date &&
                rsc.Route.Status != RouteStatus.Cancelled);

        if (hasRouteSheet)
        {
            return new DriverAvailabilityDto
            {
                DriverId = driverId,
                DriverName = driver.User.FullName,
                IsAvailable = false,
                ConflictReason = "Déjà assigné à une tournée ce jour-là",
                ConflictType = ConflictType.Route
            };
        }

        var urgentDeliveries = await context.Deliveries
            .Where(d =>
                d.UrgentDriverId == driverId &&
                d.ScheduledDate.HasValue &&
                d.ScheduledDate.Value.Date == date.Date &&
                d.Type == DeliveryType.Urgent &&
                d.Status != DeliveryStatus.Canceled)
            .CountAsync();

        if (urgentDeliveries > 0)
        {
            return new DriverAvailabilityDto
            {
                DriverId = driverId,
                DriverName = driver.User.FullName,
                IsAvailable = false,
                ConflictReason = $"A {urgentDeliveries} livraison(s) urgente(s) ce jour-là",
                ConflictType = ConflictType.UrgentDelivery
            };
        }

        return new DriverAvailabilityDto
        {
            DriverId = driverId,
            DriverName = driver.User.FullName,
            IsAvailable = true
        };
    }

    public async Task<List<DriverAvailabilityDto>> CheckMultipleAvailabilityAsync(
        List<int> driverIds, 
        DateTime date)
    {
        var results = new List<DriverAvailabilityDto>();

        foreach (var driverId in driverIds)
        {
            var availability = await CheckAvailabilityAsync(driverId, date);
            results.Add(availability);
        }

        return results;
    }

    public async Task<List<DriverDto>> GetAvailableDriversAsync(DateTime date)
    {
        // ? APPROCHE 1 : Liste explicite des statuts "occupés"
        // Un chauffeur est occupé uniquement si sa route est dans l'un de ces statuts
        var busyRouteStatuses = new[]
        {
            RouteStatus.Confirmed,  // Route confirmée
            RouteStatus.InProgress, // Route en cours
            RouteStatus.Completed   // Route terminée (le chauffeur a peut-être d'autres tâches)
        };

        var busyDriverIds = await context.RouteTeams
            .Include(rsc => rsc.Route)
            .Where(rsc => rsc.Route.Date.Date == date.Date &&
                          busyRouteStatuses.Contains(rsc.Route.Status)) // ? Inclure uniquement ces statuts
            .Select(rsc => rsc.DriverId)
            .Distinct()
            .ToListAsync();

        var urgentDriverIds = await context.Deliveries
            .Where(d => d.UrgentDriverId.HasValue &&
                        d.ScheduledDate.HasValue &&
                        d.ScheduledDate.Value.Date == date.Date &&
                        d.Type == DeliveryType.Urgent &&
                        d.Status != DeliveryStatus.Canceled)
            .Select(d => d.UrgentDriverId!.Value)
            .Distinct()
            .ToListAsync();

        var allBusyDriverIds = busyDriverIds.Union(urgentDriverIds).ToList();

        return await context.Drivers
            .Include(d => d.User)
            .Where(d => d.IsActive && !allBusyDriverIds.Contains(d.Id))
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
            .ToListAsync();
    }
}