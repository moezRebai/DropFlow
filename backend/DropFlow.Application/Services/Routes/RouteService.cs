using DropFlow.Application.Dto;
using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Routes;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Entities;
using DropFlow.Shared.Enums;
using DropFlow.Shared.Common;
using DropFlow.Shared.Routes;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Application.Services.Routes;

public class RouteService(
    IApplicationDbContext context,
    IDriverAvailabilityService availabilityService,
    IGeocodingService geocodingService,
    ITenantService tenantService,
    IRouteReferenceService routeReferenceService,
    IRouteSheetPdfGenerator pdfGenerator,
    ILogger<RouteService> logger)
    : IRouteService
{
    public async Task<PagedResult<RouteViewDto>> GetAllAsync(RouteFilterDto filter)
    {
        var query = context.Routes
            .Include(rs => rs.Vehicle)
            .Include(rs => rs.Team)
            .ThenInclude(c => c.Driver)
            .ThenInclude(d => d.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(rs => rs.Reference.Contains(term));
        }

        if (filter.Date.HasValue)
            query = query.Where(rs => rs.Date.Date == filter.Date.Value.Date);

        if (filter.Status.HasValue)
            query = query.Where(rs => rs.Status == filter.Status.Value);

        if (filter.VehicleId.HasValue)
            query = query.Where(rs => rs.VehicleId == filter.VehicleId.Value);

        if (filter.DriverId.HasValue)
            query = query.Where(rs => rs.Team.Any(c => c.DriverId == filter.DriverId.Value));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(rs => rs.Date)
            .ThenBy(rs => rs.Reference)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(rs => new RouteViewDto
            {
                Id = rs.Id,
                Reference = rs.Reference,
                Date = rs.Date,
                VehicleName = $"{rs.Vehicle.Brand} {rs.Vehicle.Model}",
                Status = rs.Status,
                StatusDisplay = rs.Status.Humanize(),
                TotalDeliveries = rs.TotalDeliveries,
                TotalDistance = rs.TotalDistance,
                TotalDuration = rs.TotalDuration,
                MainDriverName = rs.Team
                    .Where(c => c.Role == TeamMemberRole.MainDriver)
                    .Select(c => c.Driver.User.FullName)
                    .FirstOrDefault() ?? "Non assign�",
                TeamCount = rs.Team.Count
            })
            .ToListAsync();

        return new PagedResult<RouteViewDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }

    public async Task<ResponseResult<RouteDto>> GetByIdAsync(int id)
    {
        var route = await context.Routes
            .Include(rs => rs.Vehicle)
            .Include(rs => rs.Team)
            .ThenInclude(c => c.Driver)
            .ThenInclude(d => d.User)
            .Include(rs => rs.Deliveries)
            .ThenInclude(d => d.Client)
            .Include(rs => rs.Deliveries)
            .ThenInclude(d => d.ClientAddress)
            .Include(rs => rs.Deliveries)
            .ThenInclude(d => d.TimeSlot)
            .Include(rs => rs.Deliveries)
            .ThenInclude(d => d.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(rs => rs.Id == id);

        if (route == null)
            return ResponseResult<RouteDto>.Failure("Feuille de route introuvable");

        var dto = new RouteDto
        {
            Id = route.Id,
            Reference = route.Reference,
            Date = route.Date,
            VehicleId = route.VehicleId,
            VehicleName = $"{route.Vehicle.Brand} {route.Vehicle.Model}",
            Status = route.Status,
            StatusDisplay = route.Status.Humanize(),
            StartTime = route.StartTime,
            EstimatedEndTime = route.EstimatedEndTime,
            TotalDistance = route.TotalDistance,
            TotalDuration = route.TotalDuration,
            TotalDeliveries = route.TotalDeliveries,
            TotalVolume = route.TotalVolume,
            DepartureAddress = route.DepartureAddress,
            DepartureLatitude = route.DepartureLatitude,
            DepartureLongitude = route.DepartureLongitude,
            WasOptimizedByGoogle = route.WasOptimizedByGoogle,
            WasManuallyReordered = route.WasManuallyReordered,
            CreatedDate = route.CreatedDate,
            CreatedBy = route.CreatedBy,
            TeamMembers = route.Team.Select(c => new RouteTeamDto
            {
                Id = c.Id,
                DriverId = c.DriverId,
                DriverName = c.Driver.User.FullName,
                Role = c.Role,
                RoleDisplay = c.Role.Humanize()
            }).ToList(),
            Deliveries = route.Deliveries
                .OrderBy(d => d.SequenceOrder)
                .Select(d => new RouteDeliveryDto
                {
                    Id = d.Id,
                    DeliveryId = d.Id,
                    Reference = d.Reference,
                    ClientName = d.Client.DisplayName,
                    Address = $"{d.ClientAddress.Address}, {d.ClientAddress.City}",
                    SequenceOrder = d.SequenceOrder,
                    EstimatedArrivalTime = d.EstimatedArrivalTime,
                    TimeSlotId = d.TimeSlotId,
                    TimeSlotName = d.TimeSlot?.Name,
                    EstimatedDurationMinutes = d.EstimatedDurationMinutes ?? 30,
                    Longitude = d.ClientAddress.Longitude,
                    Latitude = d.ClientAddress.Latitude,
                    DepartureAddress = d.DepartureAddress,
                    DistanceToNextMeters = d.DistanceToNextMeters,
                    DepartureTime = d.DepartureTime,
                    TravelDurationMinutes = d.TravelDurationMinutes,
                    ItemCount = d.Items.Count,
                }).ToList()
        };

        return ResponseResult<RouteDto>.Success(dto);
    }

    public async Task<ResponseResult<int>> CreateAsync(CreateRouteDto dto)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Get tenant & user
            var tenantId = tenantService.GetTenantId();
            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult<int>.Failure("User not found");

            // ? 1. Validate vehicle
            var vehicle = await context.Vehicles.FindAsync(dto.VehicleId);
            if (vehicle == null)
                return ResponseResult<int>.Failure("V�hicule introuvable");
            if (!vehicle.IsActive)
                return ResponseResult<int>.Failure("V�hicule inactif");

            var isAvailable = await context.Routes
                .AllAsync(rs => rs.VehicleId != dto.VehicleId ||
                                rs.Date.Date != dto.Date.Date ||
                                rs.Status == RouteStatus.Cancelled);

            if (!isAvailable)
                return ResponseResult<int>.Failure("Ce v�hicule a d�j� une tourn�e pr�vue ce jour-l�");

            // ? 2. Generate reference
            var date = dto.Date.Date;
            var reference = await routeReferenceService.GenerateReferenceAsync(tenantId, date);

            // ? 3. Create RouteSheet
            var route = Route.Create(
                reference: reference,
                date: dto.Date,
                vehicleId: dto.VehicleId,
                startTime: dto.StartTime,
                departureAddress: dto.DepartureAddress,
                departureLatitude: dto.DepartureLatitude,
                departureLongitude: dto.DepartureLongitude);

            // Set totals & status
            route.TotalDistance = dto.TotalDistance;
            route.TotalDuration = dto.TotalDuration;
            route.TotalDeliveries = dto.Deliveries.Count;
            route.EstimatedEndTime = dto.StartTime.Add(TimeSpan.FromMinutes(dto.TotalDuration));
            route.Status = RouteStatus.Draft;

            if (dto.WasOptimizedByGoogle)
                route.MarkAsOptimizedByGoogle();
            else if (dto.WasManuallyReordered)
                route.MarkAsManuallyReordered();
            
            context.Routes.Add(route);
            await context.SaveChangesAsync(); // Get route.Id

            // ? 4. Create team
            foreach (var team in dto.Team.Select(teamDto => RouteTeam.Create(route.Id, teamDto.DriverId, teamDto.Role)))
            {
                context.RouteTeams.Add(team);
            }

            // ? 5. Update Deliveries
            foreach (var deliveryDto in dto.Deliveries)
            {
                var delivery = await context.Deliveries.FirstOrDefaultAsync(d => d.Id == deliveryDto.DeliveryId);
                if (delivery == null)
                    throw new Exception($"Livraison {deliveryDto.DeliveryId} introuvable");

                if (delivery.RouteId.HasValue)
                    throw new Exception($"Livraison {delivery.Reference} d�j� assign�e");

                // Find or create TimeSlot based on estimated arrival time
                var timeSlot = await FindOrCreateTimeSlotAsync(deliveryDto.EstimatedArrivalTime);

                // ? ROUTE ASSIGNMENT
                delivery.RouteId = route.Id;
                delivery.SequenceOrder = deliveryDto.SequenceOrder;
                delivery.EstimatedArrivalTime = deliveryDto.EstimatedArrivalTime;
                delivery.TimeSlotId = timeSlot?.Id;

                // ? ? NOUVEAUX CHAMPS - OPTIMISATION
                delivery.DepartureAddress = deliveryDto.DepartureAddress;
                delivery.DepartureTime = deliveryDto.DepartureTime;
                delivery.TravelDurationMinutes = deliveryDto.TravelDurationMinutes;
                delivery.DistanceToNextMeters = deliveryDto.DistanceToNextMeters;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation("RouteSheet {Reference} created with {DeliveryCount} deliveries and {teamCount} team",
                route.Reference, dto.Deliveries.Count, dto.Team.Count);

            return ResponseResult<int>.Success(route.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error creating route sheet");
            return ResponseResult<int>.Failure($"Erreur: {ex.Message}");
        }
    }

    public async Task<ResponseResult> UpdateAsync(int id, UpdateRouteDto dto)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var route = await context.Routes
                .Include(rs => rs.Team)
                .Include(rs => rs.Deliveries)
                .FirstOrDefaultAsync(rs => rs.Id == id);

            if (route == null)
                return ResponseResult.Failure("Feuille de route introuvable");

            // ? V�rifier que la feuille n'est pas termin�e ou annul�e
            if (route.Status is RouteStatus.Completed or RouteStatus.Cancelled)
            {
                return ResponseResult.Failure("Impossible de modifier une feuille de route termin�e ou annul�e");
            }

            // ? Seules les tourn�es Draft et Scheduled peuvent �tre modifi�es
            if (route.Status != RouteStatus.Draft && route.Status != RouteStatus.Confirmed)
            {
                return ResponseResult.Failure("Cette tourn�e ne peut plus �tre modifi�e");
            }

            logger.LogInformation("Updating route {RouteId} - Current status: {Status}", id, route.Status);

            // ? 1. Valider le v�hicule (si chang�)
            if (route.VehicleId != dto.VehicleId)
            {
                var vehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.Id == dto.VehicleId);
                if (vehicle == null || !vehicle.IsActive)
                    return ResponseResult.Failure("V�hicule introuvable ou inactif");

                // V�rifier disponibilit� du nouveau v�hicule
                var isAvailable = await context.Routes
                    .AllAsync(rs => rs.Id == id || // Exclure la route actuelle
                                    rs.VehicleId != dto.VehicleId ||
                                    rs.Date.Date != dto.Date.Date ||
                                    rs.Status == RouteStatus.Cancelled);

                if (!isAvailable)
                    return ResponseResult.Failure("Ce v�hicule a d�j� une tourn�e pr�vue ce jour-l�");

                route.VehicleId = dto.VehicleId;
            }

            // ? 2. Mettre � jour les informations de base
            route.Date = dto.Date;
            route.StartTime = dto.StartTime;
            route.DepartureAddress = dto.DepartureAddress;
            route.DepartureLatitude = dto.DepartureLatitude;
            route.DepartureLongitude = dto.DepartureLongitude;
            route.WasOptimizedByGoogle = dto.WasOptimizedByGoogle;
            route.WasManuallyReordered = dto.WasManuallyReordered;
            
            // ? 3. Mettre � jour les m�triques
            route.TotalDistance = dto.TotalDistance;
            route.TotalDuration = dto.TotalDuration;
            route.EstimatedEndTime = dto.StartTime.Add(TimeSpan.FromMinutes(dto.TotalDuration));

            // ? 4. Mettre � jour l'�quipe
            // Supprimer les anciens membres
            context.RouteTeams.RemoveRange(route.Team);

            // Ajouter les nouveaux membres
            foreach (var teamDto in dto.Team)
            {
                var driver = await context.Drivers.FirstOrDefaultAsync(dr => dr.Id == teamDto.DriverId);
                if (driver is not { IsActive: true })
                {
                    await transaction.RollbackAsync();
                    return ResponseResult.Failure($"Livreur {teamDto.DriverId} introuvable ou inactif");
                }

                var team = RouteTeam.Create(route.Id, teamDto.DriverId, teamDto.Role);
                context.RouteTeams.Add(team);
            }

            // ? 5. Mettre � jour les livraisons
            // D�tacher les anciennes livraisons
            foreach (var oldDelivery in route.Deliveries)
            {
                oldDelivery.RouteId = null;
                oldDelivery.SequenceOrder = null;
                oldDelivery.EstimatedArrivalTime = null;
                oldDelivery.TimeSlotId = null;

                oldDelivery.DepartureAddress = null;
                oldDelivery.DepartureTime = null;
                oldDelivery.TravelDurationMinutes = null;
                oldDelivery.DistanceToNextMeters = null;
            }

            // Attacher les nouvelles livraisons
            foreach (var deliveryDto in dto.Deliveries)
            {
                var delivery = await context.Deliveries.FirstOrDefaultAsync(d => d.Id == deliveryDto.DeliveryId);
                if (delivery == null)
                {
                    await transaction.RollbackAsync();
                    return ResponseResult.Failure($"Livraison {deliveryDto.DeliveryId} introuvable");
                }

                // V�rifier qu'elle n'est pas d�j� dans une autre route
                if (delivery.RouteId.HasValue && delivery.RouteId != route.Id)
                {
                    await transaction.RollbackAsync();
                    return ResponseResult.Failure($"Livraison {delivery.Reference} d�j� assign�e � une autre tourn�e");
                }

                // Find or create TimeSlot based on estimated arrival time
                var timeSlot = deliveryDto.EstimatedArrivalTime.HasValue
                    ? await FindOrCreateTimeSlotAsync(deliveryDto.EstimatedArrivalTime.Value)
                    : null;

                delivery.RouteId = route.Id;
                delivery.SequenceOrder = deliveryDto.SequenceOrder;
                delivery.EstimatedArrivalTime = deliveryDto.EstimatedArrivalTime;
                delivery.TimeSlotId = timeSlot?.Id;

                // ? ? NOUVEAUX CHAMPS - OPTIMISATION
                delivery.DepartureAddress = deliveryDto.DepartureAddress;
                delivery.DepartureTime = deliveryDto.DepartureTime;
                delivery.TravelDurationMinutes = deliveryDto.TravelDurationMinutes;
                delivery.DistanceToNextMeters = deliveryDto.DistanceToNextMeters;
            }

            // ? 6. Mettre � jour le compteur de livraisons
            route.TotalDeliveries = dto.Deliveries.Count;
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation(
                "Route {RouteId} updated successfully - Vehicle: {VehicleId}, Team: {TeamCount}, Deliveries: {DeliveryCount}",
                route.Id, route.VehicleId, dto.Team.Count, dto.Deliveries.Count);

            return ResponseResult.Success("Tourn�e mise � jour avec succ�s");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error updating route {RouteId}", id);
            return ResponseResult.Failure($"Erreur lors de la modification: {ex.Message}");
        }
    }

    public async Task<ResponseResult> DeleteAsync(int id)
    {
        try
        {
            var route = await context.Routes
                .Include(rs => rs.Deliveries)
                .Include(rs => rs.Team) // ? AJOUT: Charger l'�quipe
                .FirstOrDefaultAsync(rs => rs.Id == id);

            if (route == null)
                return ResponseResult.Failure("Feuille de route introuvable");

            // ? CORRECTION: Seules les tourn�es Draft peuvent �tre supprim�es
            if (route.Status != RouteStatus.Draft)
                return ResponseResult.Failure("Seules les tourn�es en brouillon peuvent �tre supprim�es");

            // ? CORRECTION: Lib�rer les livraisons
            foreach (var delivery in route.Deliveries)
            {
                delivery.RouteId = null;
                delivery.SequenceOrder = null;
                delivery.EstimatedArrivalTime = null;
                delivery.TimeSlotId = null; // ? AJOUT: Retirer le cr�neau

                logger.LogInformation("Delivery {Reference} freed from route {RouteId}",
                    delivery.Reference, id);
            }

            // ? CORRECTION: Supprimer les membres d'�quipe (lib�re les chauffeurs)
            context.RouteTeams.RemoveRange(route.Team);

            logger.LogInformation("Team members removed from route {RouteId}", id);

            // ? CORRECTION: Supprimer compl�tement la tourn�e (pas juste Cancel)
            context.Routes.Remove(route);
            await context.SaveChangesAsync();

            logger.LogInformation("RouteSheet {Reference} deleted and resources freed", route.Reference);

            return ResponseResult.Success("Tourn�e supprim�e avec succ�s");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting route sheet {Id}", id);
            return ResponseResult.Failure("Erreur lors de la suppression");
        }
    }

    public async Task<ResponseResult> AddTeamMemberAsync(int routeId, TeamMemberDto dto)
    {
        try
        {
            var route = await context.Routes
                .Include(rs => rs.Team)
                .FirstOrDefaultAsync(rs => rs.Id == routeId);

            if (route == null)
                return ResponseResult.Failure("Feuille de route introuvable");

            if (route.Status != RouteStatus.Draft)
                return ResponseResult.Failure("Impossible de modifier l'�quipage d'une tourn�e confirm�e");

            // Check driver exists
            var driver = await context.Drivers.FirstOrDefaultAsync(dr => dr.Id == dto.DriverId);
            if (driver == null || !driver.IsActive)
                return ResponseResult.Failure("Livreur introuvable ou inactif");

            // Check already in team
            if (route.Team.Any(c => c.DriverId == dto.DriverId))
                return ResponseResult.Failure("Ce livreur est d�j� dans l'�quipage");

            // Validate team size
            if (route.Team.Count >= 3)
                return ResponseResult.Failure("L'�quipage ne peut pas d�passer 3 personnes");

            // Validate main driver uniqueness
            if (dto.Role == TeamMemberRole.MainDriver && route.Team.Any(c => c.Role == TeamMemberRole.MainDriver))
                return ResponseResult.Failure("Il ne peut y avoir qu'un seul chauffeur principal");

            // Check availability
            var availability = await availabilityService.CheckAvailabilityAsync(dto.DriverId, route.Date);
            if (!availability.IsAvailable)
                return ResponseResult.Failure($"Livreur non disponible : {availability.ConflictReason}");

            var team = RouteTeam.Create(routeId, dto.DriverId, dto.Role);
            context.RouteTeams.Add(team);
            await context.SaveChangesAsync();

            return ResponseResult.Success("Livreur ajout� � l'�quipage");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding team member");
            return ResponseResult.Failure("Erreur lors de l'ajout du livreur");
        }
    }

    public async Task<ResponseResult> RemoveTeamMemberAsync(int routeId, int driverId)
    {
        try
        {
            var team = await context.RouteTeams
                .FirstOrDefaultAsync(c => c.RouteId == routeId && c.DriverId == driverId);

            if (team == null)
                return ResponseResult.Failure("Membre d'�quipage introuvable");

            var route = await context.Routes.FirstOrDefaultAsync(r => r.Id == routeId);
            if (route?.Status != RouteStatus.Draft)
                return ResponseResult.Failure("Impossible de modifier l'�quipage d'une tourn�e confirm�e");

            context.RouteTeams.Remove(team);
            await context.SaveChangesAsync();

            return ResponseResult.Success("Livreur retir� de l'�quipage");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing team member");
            return ResponseResult.Failure("Erreur lors du retrait du livreur");
        }
    }

    public async Task<ResponseResult> AddDeliveryAsync(int routeId, int deliveryId)
    {
        try
        {
            var route = await context.Routes
                .Include(rs => rs.Vehicle)
                .Include(rs => rs.Deliveries)
                .FirstOrDefaultAsync(rs => rs.Id == routeId);

            if (route == null)
                return ResponseResult.Failure("Feuille de route introuvable");

            if (route.Status != RouteStatus.Draft)
                return ResponseResult.Failure("Impossible d'ajouter des livraisons � une tourn�e confirm�e");

            var delivery = await context.Deliveries.FirstOrDefaultAsync(d => d.Id == deliveryId);
            if (delivery == null)
                return ResponseResult.Failure("Livraison introuvable");

            if (delivery.Type == DeliveryType.Urgent)
                return ResponseResult.Failure("Les livraisons urgentes ne peuvent pas �tre ajout�es � une tourn�e");

            if (delivery.RouteId.HasValue)
                return ResponseResult.Failure("Cette livraison est d�j� dans une tourn�e");

            // Check vehicle capacity
            if (route.Deliveries.Count >= route.Vehicle.MaxDeliveries)
                return ResponseResult.Failure(
                    $"Capacit� maximale du v�hicule atteinte ({route.Vehicle.MaxDeliveries} livraisons)");

            delivery.RouteId = routeId;
            delivery.SequenceOrder = route.Deliveries.Count + 1;

            await context.SaveChangesAsync();
            await RecalculateMetricsInternalAsync(routeId);

            return ResponseResult.Success("Livraison ajout�e � la tourn�e");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding delivery to route sheet");
            return ResponseResult.Failure("Erreur lors de l'ajout de la livraison");
        }
    }

    public async Task<ResponseResult> RemoveDeliveryAsync(int routeId, int deliveryId)
    {
        try
        {
            var delivery = await context.Deliveries
                .FirstOrDefaultAsync(d => d.Id == deliveryId && d.RouteId == routeId);

            if (delivery == null)
                return ResponseResult.Failure("Livraison introuvable dans cette tourn�e");

            var route = await context.Routes.FirstOrDefaultAsync(r => r.Id == routeId);
            if (route?.Status != RouteStatus.Draft)
                return ResponseResult.Failure("Impossible de retirer des livraisons d'une tourn�e confirm�e");

            delivery.RouteId = null;
            delivery.SequenceOrder = null;
            delivery.EstimatedArrivalTime = null;

            await context.SaveChangesAsync();
            await RecalculateMetricsInternalAsync(routeId);

            return ResponseResult.Success("Livraison retir�e de la tourn�e");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing delivery from route sheet");
            return ResponseResult.Failure("Erreur lors du retrait de la livraison");
        }
    }

    public async Task<ResponseResult> UpdateSequenceAsync(int routeId, List<UpdateDeliverySequenceDto> sequences)
    {
        try
        {
            var route = await context.Routes
                .Include(rs => rs.Deliveries)
                .FirstOrDefaultAsync(rs => rs.Id == routeId);

            if (route == null)
                return ResponseResult.Failure("Feuille de route introuvable");

            if (route.Status != RouteStatus.Draft)
                return ResponseResult.Failure("Impossible de r�ordonner une tourn�e confirm�e");

            foreach (var seq in sequences)
            {
                var delivery = route.Deliveries.FirstOrDefault(d => d.Id == seq.DeliveryId);
                if (delivery != null)
                    delivery.SequenceOrder = seq.SequenceOrder;
            }

            await context.SaveChangesAsync();

            return ResponseResult.Success("Ordre des livraisons mis � jour");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating sequence");
            return ResponseResult.Failure("Erreur lors de la mise � jour de l'ordre");
        }
    }

    public async Task<ResponseResult> ConfirmAsync(int id)
    {
        try
        {
            var route = await context.Routes
                .Include(rs => rs.Team)
                .Include(rs => rs.Deliveries)
                .FirstOrDefaultAsync(rs => rs.Id == id);

            if (route == null)
                return ResponseResult.Failure("Feuille de route introuvable");

            // ? CORRECTION: V�rifier le statut actuel
            if (route.Status != RouteStatus.Draft)
                return ResponseResult.Failure("Seules les tourn�es en brouillon peuvent �tre confirm�es");

            // Validations
            if (route.Team.All(c => c.Role != TeamMemberRole.MainDriver))
                return ResponseResult.Failure("Un chauffeur principal est requis");

            if (route.Team.Count < 2)
                return ResponseResult.Failure("Au moins 2 membres d'�quipage sont requis");

            if (!route.Deliveries.Any())
                return ResponseResult.Failure("Au moins une livraison est requise");

            // ? CORRECTION: Utiliser Scheduled au lieu de Confirmed
            route.Confirm(); // Appelle la m�thode du domain

            // Si la m�thode domain met � Confirmed, forcer � Scheduled
            if (route.Status == RouteStatus.Confirmed)
            {
                route.Status = RouteStatus.Confirmed;
            }

            await context.SaveChangesAsync();

            logger.LogInformation("Route {Reference} confirmed and set to Scheduled status", route.Reference);

            return ResponseResult.Success("Tourn�e confirm�e");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error confirming route sheet");
            return ResponseResult.Failure("Erreur lors de la confirmation");
        }
    }

    public async Task<ResponseResult> StartAsync(int id)
    {
        try
        {
            var route = await context.Routes.FirstOrDefaultAsync(r => r.Id == id);
            if (route == null)
                return ResponseResult.Failure("Feuille de route introuvable");

            // ? CORRECTION: V�rifier le statut actuel
            if (route.Status != RouteStatus.Confirmed)
                return ResponseResult.Failure("Seules les tourn�es planifi�es peuvent �tre d�marr�es");

            route.Start();
            await context.SaveChangesAsync();

            logger.LogInformation("Route {Id} started", id);

            return ResponseResult.Success("Tourn�e d�marr�e");
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting route {Id}", id);
            return ResponseResult.Failure("Erreur lors du d�marrage");
        }
    }

    public async Task<ResponseResult> CompleteAsync(int id)
    {
        try
        {
            var route = await context.Routes.FirstOrDefaultAsync(r => r.Id == id);
            if (route == null)
                return ResponseResult.Failure("Feuille de route introuvable");

            // ? CORRECTION: V�rifier le statut actuel
            if (route.Status != RouteStatus.InProgress)
                return ResponseResult.Failure("Seules les tourn�es en cours peuvent �tre termin�es");

            route.Complete();
            await context.SaveChangesAsync();

            logger.LogInformation("Route {Id} completed", id);

            return ResponseResult.Success("Tourn�e termin�e");
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error completing route {Id}", id);
            return ResponseResult.Failure("Erreur lors de la finalisation");
        }
    }

    public async Task<ResponseResult> CancelAsync(int id)
    {
        try
        {
            var route = await context.Routes
                .Include(rs => rs.Deliveries) // ? AJOUT: Charger les livraisons
                .Include(rs => rs.Team) // ? AJOUT: Charger l'�quipe
                .FirstOrDefaultAsync(rs => rs.Id == id);

            if (route == null)
                return ResponseResult.Failure("Feuille de route introuvable");

            // ? CORRECTION: Interdire annulation si termin�e
            if (route.Status == RouteStatus.Completed)
                return ResponseResult.Failure("Impossible d'annuler une tourn�e termin�e");

            // ? AJOUT: Sauvegarder le statut pr�c�dent pour les logs
            var previousStatus = route.Status;

            // ? CORRECTION: Lib�rer TOUTES les ressources

            // 1. Lib�rer les livraisons
            foreach (var delivery in route.Deliveries)
            {
                delivery.RouteId = null;
                delivery.SequenceOrder = null;
                delivery.EstimatedArrivalTime = null;
                delivery.TimeSlotId = null; // ? Important: retirer le cr�neau

                logger.LogInformation("Delivery {Reference} freed from cancelled route {RouteId}",
                    delivery.Reference, id);
            }

            // 2. Supprimer les membres d'�quipe (lib�re les chauffeurs)
            if (route.Team.Any())
            {
                context.RouteTeams.RemoveRange(route.Team);
                logger.LogInformation("{Count} team members freed from cancelled route {RouteId}",
                    route.Team.Count, id);
            }

            // 3. Marquer la tourn�e comme annul�e
            route.Cancel();

            await context.SaveChangesAsync();

            logger.LogInformation(
                "Route {Reference} cancelled. Previous status: {PreviousStatus}. {DeliveryCount} deliveries freed, {TeamCount} team members freed",
                route.Reference,
                previousStatus,
                route.Deliveries.Count,
                route.Team.Count);

            return ResponseResult.Success("Tourn�e annul�e et ressources lib�r�es");
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling route {Id}", id);
            return ResponseResult.Failure("Erreur lors de l'annulation");
        }
    }

    public async Task<ResponseResult<OptimizePathResponseDto>> OptimizeRouteAsync(OptimizePathRequestDto request)
    {
        try
        {
            // ? 1. Validation des param�tres
            if (string.IsNullOrWhiteSpace(request.DepartureAddress))
                return ResponseResult<OptimizePathResponseDto>.Failure("L'adresse de d�part est obligatoire");

            if (request.DeliveryIds.Count == 0)
                return ResponseResult<OptimizePathResponseDto>.Failure("Aucune livraison � optimiser");

            // ? 2. Charger les livraisons avec leurs adresses
            var deliveries = await context.Deliveries
                .Include(d => d.ClientAddress)
                .Where(d => request.DeliveryIds.Contains(d.Id))
                .ToListAsync();

            if (deliveries.Count == 0)
                return ResponseResult<OptimizePathResponseDto>.Failure("Aucune livraison trouv�e");

            // ? 3. V�rifier que toutes les livraisons ont des coordonn�es GPS
            var deliveriesWithoutCoords = deliveries
                .Where(d => !d.ClientAddress.Latitude.HasValue || !d.ClientAddress.Longitude.HasValue)
                .ToList();

            if (deliveriesWithoutCoords.Any())
            {
                var references = string.Join(", ", deliveriesWithoutCoords.Select(d => d.Reference));
                return ResponseResult<OptimizePathResponseDto>.Failure(
                    $"Certaines livraisons n'ont pas de coordonn�es GPS: {references}");
            }

            // ? 4. CAS SP�CIAL : Une seule livraison
            if (deliveries.Count == 1)
            {
                var singleDelivery = deliveries[0];

                // Construire l'adresse de destination (coordonn�es GPS)
                var destination =
                    $"{singleDelivery.ClientAddress.Latitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                    $"{singleDelivery.ClientAddress.Longitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

                logger.LogInformation("Calculating route for single delivery from {Origin} to {Destination}",
                    request.DepartureAddress, destination);

                // Appeler l'API Google Directions (simple route, pas d'optimisation)
                var (singleRouteResponse, singleRouteError) =
                    await geocodingService.GetDirectionsAsync(request.DepartureAddress, destination);

                if (!string.IsNullOrEmpty(singleRouteError))
                {
                    logger.LogWarning("Error calculating route for single delivery: {Error}", singleRouteError);
                    return ResponseResult<OptimizePathResponseDto>.Failure(singleRouteError);
                }

                // V�rifier la r�ponse
                if (singleRouteResponse?.Routes == null || singleRouteResponse.Routes.Count == 0)
                {
                    logger.LogWarning("No routes found for single delivery");
                    return ResponseResult<OptimizePathResponseDto>.Failure(
                        "Aucun itin�raire trouv�. V�rifiez les adresses.");
                }

                var route = singleRouteResponse.Routes[0];

                if (route.Legs == null || !route.Legs.Any())
                {
                    logger.LogWarning("No legs found in route for single delivery");
                    return ResponseResult<OptimizePathResponseDto>.Failure(
                        "Impossible de calculer la distance. V�rifiez les adresses.");
                }

                // R�cup�rer les donn�es du leg (segment de route)
                var leg = route.Legs[0];
                var distanceMeters = leg.Distance?.Value ?? 0;
                var durationSeconds = leg.Duration?.Value ?? 0;
                var durationMinutes = durationSeconds / 60;

                // ? Calculer dur�e totale = trajet + prestation
                // La dur�e de prestation est stock�e dans EstimatedDurationMinutes de la livraison
                var serviceDurationMinutes = singleDelivery.EstimatedDurationMinutes ?? 15; // D�faut 15 min
                var totalDurationMinutes = durationMinutes + serviceDurationMinutes;

                logger.LogInformation(
                    "Single delivery route: {Distance}m, {Duration}min travel + {Service}min service = {Total}min total",
                    distanceMeters, durationMinutes, serviceDurationMinutes, totalDurationMinutes);

                // Construire la r�ponse
                var singleResponse = new OptimizePathResponseDto
                {
                    Deliveries =
                    [
                        new OptimizedDeliveryDto
                        {
                            DeliveryId = singleDelivery.Id,
                            SequenceOrder = 1,
                            Address = singleDelivery.ClientAddress.FullAddress,
                            Latitude = singleDelivery.ClientAddress.Latitude.Value,
                            Longitude = singleDelivery.ClientAddress.Longitude.Value,
                            DistanceToNextMeters = distanceMeters,
                            DurationToNextMinutes = durationMinutes
                        }
                    ],
                    TotalDistanceKm = distanceMeters / 1000m,
                    TotalDurationMinutes = totalDurationMinutes // ? Trajet + prestation
                };

                return ResponseResult<OptimizePathResponseDto>.Success(singleResponse);
            }

            // ? 5. CAS MULTIPLE : Plusieurs livraisons - Optimisation via Google

            // Construire la cha�ne de waypoints (coordonn�es GPS s�par�es par |)
            var waypoints = string.Join("|", deliveries.Select(d =>
                $"{d.ClientAddress.Latitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                $"{d.ClientAddress.Longitude!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));

            logger.LogInformation("Optimizing route with {Count} deliveries", deliveries.Count);

            // Appeler l'API Google Directions avec optimisation
            var (googleDirectionsResponse, errorMessage) =
                await geocodingService.GetOptimizedRouteAsync(request.DepartureAddress, waypoints, optimize: true);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                logger.LogError("Google Directions API error: {Error}", errorMessage);
                return ResponseResult<OptimizePathResponseDto>.Failure(errorMessage);
            }

            // ? 6. Validation de la r�ponse Google
            if (googleDirectionsResponse.Routes == null || !googleDirectionsResponse.Routes.Any())
            {
                return ResponseResult<OptimizePathResponseDto>.Failure(
                    "Aucun itin�raire trouv�. V�rifiez les adresses.");
            }

            var optimizedRoute = googleDirectionsResponse.Routes[0];

            if (optimizedRoute.WaypointOrder == null)
            {
                return ResponseResult<OptimizePathResponseDto>.Failure(
                    "Erreur dans l'ordre des points. R�essayez.");
            }

            if (optimizedRoute.Legs == null || !optimizedRoute.Legs.Any())
            {
                return ResponseResult<OptimizePathResponseDto>.Failure(
                    "Erreur dans les segments de route. R�essayez.");
            }

            if (optimizedRoute.WaypointOrder.Length != deliveries.Count)
            {
                logger.LogError("Waypoint order length mismatch: {Expected} expected, {Actual} received",
                    deliveries.Count, optimizedRoute.WaypointOrder.Length);
                return ResponseResult<OptimizePathResponseDto>.Failure(
                    "Erreur dans l'ordre optimis�. R�essayez.");
            }

            // ? 7. Construire la liste des livraisons optimis�es
            var optimizedDeliveries = new List<OptimizedDeliveryDto>();

            for (var i = 0; i < optimizedRoute.WaypointOrder.Length; i++)
            {
                var originalIndex = optimizedRoute.WaypointOrder[i];

                // V�rifier que l'index est valide
                if (originalIndex < 0 || originalIndex >= deliveries.Count)
                {
                    logger.LogError("Invalid waypoint order index: {Index}", originalIndex);
                    return ResponseResult<OptimizePathResponseDto>.Failure(
                        "Erreur dans l'ordre des points. R�essayez.");
                }

                var delivery = deliveries[originalIndex];

                // V�rifier que le leg correspondant existe
                if (i >= optimizedRoute.Legs.Count)
                {
                    logger.LogError("Leg index out of bounds: {Index}", i);
                    return ResponseResult<OptimizePathResponseDto>.Failure(
                        "Erreur dans les segments de route. R�essayez.");
                }

                var leg = optimizedRoute.Legs[i];

                optimizedDeliveries.Add(new OptimizedDeliveryDto
                {
                    DeliveryId = delivery.Id,
                    SequenceOrder = i + 1,
                    Address = delivery.ClientAddress.FullAddress,
                    Latitude = delivery.ClientAddress.Latitude.Value,
                    Longitude = delivery.ClientAddress.Longitude.Value,
                    DistanceToNextMeters = leg.Distance?.Value ?? 0,
                    DurationToNextMinutes = (leg.Duration?.Value ?? 0) / 60
                });
            }

            // ? 8. Calculer les totaux (premiers n legs seulement, le n+1 est le retour au d�p�t)
            var totalDistanceKm = optimizedRoute.Legs.Take(deliveries.Count).Sum(l => l.Distance?.Value ?? 0) / 1000m;
            var totalTravelMinutes = optimizedRoute.Legs.Take(deliveries.Count).Sum(l => l.Duration?.Value ?? 0) / 60;

            // ? Ajouter les dur�es de prestation de chaque livraison
            var totalServiceMinutes = deliveries.Sum(d => d.EstimatedDurationMinutes ?? 15);
            var totalMinutes = totalTravelMinutes + totalServiceMinutes;

            logger.LogInformation(
                "Route optimized: {Distance}km, {Travel}min travel + {Service}min service = {Total}min total",
                totalDistanceKm, totalTravelMinutes, totalServiceMinutes, totalMinutes);

            // ? 9. Construire la r�ponse
            var response = new OptimizePathResponseDto
            {
                Deliveries = optimizedDeliveries,
                TotalDistanceKm = totalDistanceKm,
                TotalDurationMinutes = totalMinutes // ? Trajet + prestations
            };

            return ResponseResult<OptimizePathResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error optimizing route");
            return ResponseResult<OptimizePathResponseDto>.Failure(
                "Erreur inattendue lors de l'optimisation. R�essayez.");
        }
    }

    /// <summary>
    /// Recalcule les distances/dur�es en GARDANT l'ordre actuel des livraisons
    /// Utilis� quand le manager a r�organis� manuellement
    /// </summary>
    public async Task<ResponseResult<OptimizePathResponseDto>> RecalculateRouteMetricsAsync(
        OptimizePathRequestDto request)
    {
        try
        {
            logger.LogInformation("?? Recalcul des m�triques (sans optimisation) pour {Count} livraisons",
                request.DeliveryIds.Count);

            // --- 1. VALIDATION ---
            if (request.DeliveryIds == null || !request.DeliveryIds.Any())
            {
                return ResponseResult<OptimizePathResponseDto>.Failure("Aucune livraison s�lectionn�e");
            }

            if (string.IsNullOrWhiteSpace(request.DepartureAddress))
            {
                return ResponseResult<OptimizePathResponseDto>.Failure("Adresse de d�part requise");
            }

            // --- 2. R�CUP�RER LES LIVRAISONS DANS L'ORDRE DONN� ---
            // --- 2. R�CUP�RER LES LIVRAISONS (1 seule requ�te SQL) ---
            var deliveries = await context.Deliveries
                .Include(d => d.ClientAddress)
                .Where(d => request.DeliveryIds.Contains(d.Id))
                .ToListAsync();

            // V�rification compl�tude
            if (deliveries.Count != request.DeliveryIds.Count)
            {
                var found = deliveries.Select(d => d.Id).ToList();
                var missing = request.DeliveryIds.Except(found).ToList();
            
                logger.LogWarning("Livraisons introuvables: {Missing}", string.Join(", ", missing));
            
                return ResponseResult<OptimizePathResponseDto>.Failure(
                    $"Livraisons introuvables: {string.Join(", ", missing)}");
            }

            // ?? CRUCIAL: R�ordonner selon l'ordre manuel de request.DeliveryIds
            var orderedDeliveries = request.DeliveryIds
                .Select(id => deliveries.First(d => d.Id == id))
                .ToList();

            logger.LogInformation("Ordre manuel pr�serv�: {Order}", 
                string.Join(" ? ", orderedDeliveries.Select(d => d.Reference)));

            // --- 3. V�RIFIER LES COORDONN�ES GPS ---
            var missingCoordinates = deliveries
                .Where(d => !d.ClientAddress.Latitude.HasValue || !d.ClientAddress.Longitude.HasValue)
                .Select(d => d.Reference)
                .ToList();

            if (missingCoordinates.Any())
            {
                return ResponseResult<OptimizePathResponseDto>.Failure(
                    $"Coordonn�es GPS manquantes pour: {string.Join(", ", missingCoordinates)}");
            }

            // --- 4. CONSTRUIRE LES WAYPOINTS (dans l'ordre donn�) ---
            var wayPoints = string.Join("|", orderedDeliveries.Select(d =>
                $"{d.ClientAddress.Latitude!.Value},{d.ClientAddress.Longitude!.Value}"));

            logger.LogInformation("Waypoints (ordre manuel): {WayPoints}", wayPoints);

            // --- 5. APPEL GOOGLE API AVEC optimize:false ---
            var (googleResponse, error) = await geocodingService.GetOptimizedRouteAsync(
                request.DepartureAddress,
                wayPoints,
                optimize: false); // ? NE PAS optimiser, garder l'ordre

            if (!string.IsNullOrEmpty(error))
            {
                logger.LogError("Erreur Google Directions API: {Error}", error);
                return ResponseResult<OptimizePathResponseDto>.Failure(error);
            }

            if (googleResponse.Status != "OK" ||
                googleResponse.Routes == null ||
                !googleResponse.Routes.Any())
            {
                return ResponseResult<OptimizePathResponseDto>.Failure(
                    $"Erreur Google Maps: {googleResponse.Status}");
            }

            var route = googleResponse.Routes[0];
            var legs = route.Legs;

            // With optimize:false Google returns waypoint_order:[] (empty) and
            // legs.Count = n+1 because destination = last waypoint coord produces
            // a zero-distance final leg. We need at least n legs (one per delivery).
            if (legs == null || legs.Count < orderedDeliveries.Count)
            {
                logger.LogError(
                    "Nombre de legs ({LegsCount}) insuffisant pour {DeliveriesCount} livraisons",
                    legs?.Count ?? 0, orderedDeliveries.Count);
                return ResponseResult<OptimizePathResponseDto>.Failure(
                    "Erreur de calcul d'itin�raire");
            }

            // --- 6. CONSTRUIRE LE R�SULTAT (DANS L'ORDRE DONN�) ---
            // Only consume the first n legs � the n+1th is a zero-distance return leg.
            var optimizedDeliveries = new List<OptimizedDeliveryDto>();

            for (int i = 0; i < orderedDeliveries.Count; i++)
            {
                var delivery = orderedDeliveries[i];
                var leg = legs[i];

                optimizedDeliveries.Add(new OptimizedDeliveryDto
                {
                    DeliveryId = delivery.Id,
                    SequenceOrder = i + 1,
                    Address = delivery.ClientAddress.FullAddress,
                    Latitude = delivery.ClientAddress.Latitude.Value,
                    Longitude = delivery.ClientAddress.Longitude.Value,
                    DistanceToNextMeters = leg.Distance?.Value ?? 0,
                    DurationToNextMinutes = (leg.Duration?.Value ?? 0) / 60
                });
            }

            // --- 7. CALCULER LES TOTAUX (premiers n legs seulement) ---
            var totalDistanceMeters = legs.Take(orderedDeliveries.Count).Sum(leg => leg.Distance?.Value ?? 0);
            var totalDurationSeconds = legs.Take(orderedDeliveries.Count).Sum(leg => leg.Duration?.Value ?? 0);

            var response = new OptimizePathResponseDto
            {
                Deliveries = optimizedDeliveries,
                TotalDistanceKm = totalDistanceMeters / 1000m,
                TotalDurationMinutes = totalDurationSeconds / 60
            };

            logger.LogInformation(
                "? Recalcul termin�: {Distance} km, {Duration} min, {Count} livraisons",
                response.TotalDistanceKm, response.TotalDurationMinutes, response.Deliveries.Count);

            return ResponseResult<OptimizePathResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors du recalcul des m�triques");
            return ResponseResult<OptimizePathResponseDto>.Failure(
                $"Erreur lors du recalcul: {ex.Message}");
        }
    }

    public async Task<ResponseResult<byte[]>> GenerateRouteSheetPdfAsync(int routeId)
    {
        try
        {
            // 1. R�cup�rer la route avec toutes les donn�es n�cessaires
            var route = await context.Routes
                .Include(r => r.Vehicle)
                .Include(r => r.Deliveries.OrderBy(d => d.SequenceOrder))
                .ThenInclude(d => d.Client)
                .Include(r => r.Deliveries)
                .ThenInclude(d => d.ClientAddress)
                .Include(r => r.Deliveries)
                .ThenInclude(d => d.Store)
                .Include(r => r.Deliveries)
                .ThenInclude(d => d.TimeSlot)
                .Include(r => r.Team)
                .ThenInclude(rt => rt.Driver)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(r => r.Id == routeId);

            if (route == null)
                return ResponseResult<byte[]>.Failure("Route non trouv�e");

            // 2. R�cup�rer les informations de l'entreprise (tenant)
            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantService.GetTenantId());

            if (tenant == null)
                return ResponseResult<byte[]>.Failure("Informations entreprise non trouv�es");

            // 3. Cr�er l'acronyme de l'entreprise pour le PDF
            var companyAcronym = GetCompanyAcronym(tenant.CompanyName ?? tenant.Name);

            // 4. Mapper vers le DTO
            var routeSheetDto = new RouteSheetDto
            {
                // Informations entreprise
                CompanyName = tenant.CompanyName ?? tenant.Name,
                CompanyAddress = tenant.Address ?? "",
                CompanyCity = $"{tenant.ZipCode} {tenant.City}",
                CompanyPhone = tenant.Phone ?? "",
                CompanySiret = tenant.Siret ?? "",
                CompanyLogoUrl = tenant.LogoUrl,

                // Informations route
                RouteReference = route.Reference,
                VehicleName = route.Vehicle?.PlateNumber ?? route.Vehicle?.Model ?? "N/A",
                RouteDate = route.Date,
                DepartureAddress = route.DepartureAddress ?? "",
                DepartureTime = route.StartTime,

                // �quipe
                TeamMembers = string.Join(", ", route.Team.Select(tm =>
                {
                    var driver = tm.Driver;
                    var role = tm.Role == TeamMemberRole.MainDriver ? "Chauffeur" : "Monteur";
                    return $"{driver.User.FirstName} {driver.User.LastName} ({role})";
                })),

                // Acronyme de l'entreprise pour les colonnes
                CompanyAcronym = companyAcronym,

                // Livraisons
                Deliveries = route.Deliveries
                    .Where(d => d.SequenceOrder.HasValue)
                    .OrderBy(d => d.SequenceOrder)
                    .Select(d => new RouteSheetDeliveryDto
                    {
                        SequenceOrder = d.SequenceOrder ?? 0,
                        DeliveryReference = d.Reference, // N� Dossier
                        ClientName = d.Client.DisplayName,
                        ClientPhone = d.Client.Phone ?? "",
                        City = d.ClientAddress.City,
                        FullAddress = $"{d.ClientAddress.FullAddress}",
                        EstimatedArrivalTime = d.EstimatedArrivalTime,

                        // Type de prestation (M = Montage, N = Normal)
                        ServiceType = d.WithAssembly ? "M" : "N",

                        // Enseigne (magasin)
                        StoreName = d.Store?.Name ?? "",

                        // Montants
                        StorePaymentAmount = d.StorePaymentAmount ?? 0m, // CRT MAG
                        ClientPaymentAmount = d.ClientPaymentAmount ?? 0m, // CRT {CompanyAcronym}

                        // Instructions
                        Instructions = d.DeliveryNotes ?? "",

                        // Cr�neaux horaires (si TimeSlot existe)
                        TimeSlotStart = d.TimeSlot?.StartTime,
                        TimeSlotEnd = d.TimeSlot?.EndTime
                    }).ToList(),

                // Totaux
                TotalStorePayment = route.Deliveries
                    .Where(d => d.SequenceOrder.HasValue)
                    .Sum(d => d.StorePaymentAmount ?? 0m),

                TotalClientPayment = route.Deliveries
                    .Where(d => d.SequenceOrder.HasValue)
                    .Sum(d => d.ClientPaymentAmount ?? 0m),

                // QR Code - Lien vers site web de l'entreprise
                QrCodeData = tenant.Website ?? $"https://{tenant.SubDomain}.dropflow.com",

                // Notes
                Notes = "La pause d'une heure est obligatoire dans la journ�e. Effectuer plusieurs tours si n�cessaire"
            };

            // 5. G�n�rer le PDF
            var pdfBytes = await pdfGenerator.GenerateAsync(routeSheetDto);

            logger.LogInformation(
                "Feuille de route PDF g�n�r�e pour la route {RouteId} - {RouteReference}",
                routeId,
                route.Reference);

            return ResponseResult<byte[]>.Success(pdfBytes);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Erreur lors de la g�n�ration de la feuille de route PDF pour la route {RouteId}",
                routeId);

            return ResponseResult<byte[]>.Failure($"Erreur lors de la g�n�ration du PDF: {ex.Message}");
        }
    }

    /// <summary>
    /// Cr�e un acronyme � partir du nom de l'entreprise
    /// Ex: "SRS Services" => "SRS", "Soci�t� de Transport" => "SDT"
    /// </summary>
    private static string GetCompanyAcronym(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return "SRS"; // Valeur par d�faut

        // Prendre les initiales des mots (max 3-4 lettres)
        var words = companyName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2) // Ignorer les mots courts comme "de", "la", etc.
            .Take(3) // Max 3 mots
            .Select(w => w[0].ToString().ToUpper())
            .ToArray();

        return words.Length == 0 ?
            // Si aucun mot valide, prendre les 3 premi�res lettres du nom
            companyName[..Math.Min(3, companyName.Length)].ToUpper() : string.Join("", words);
    }

    private async Task RecalculateMetricsInternalAsync(int routeId)
    {
        var route = await context.Routes
            .Include(rs => rs.Deliveries)
            .FirstOrDefaultAsync(rs => rs.Id == routeId);

        if (route == null) return;

        var totalDeliveries = route.Deliveries.Count;
        var totalDuration = route.Deliveries.Sum(d => d.EstimatedDurationMinutes ?? 30);

        route.UpdateMetrics(
            totalDistance: 0, // � calculer avec Google Maps
            totalDuration: totalDuration,
            totalDeliveries: totalDeliveries,
            totalVolume: 0,
            estimatedEndTime: route.StartTime.Add(TimeSpan.FromMinutes(totalDuration))
        );

        await context.SaveChangesAsync();
    }

    private async Task<TimeSlot> FindOrCreateTimeSlotAsync(TimeSpan arrivalTime)
    {
        // Match against a manager-defined slot that covers the arrival time
        var existing = await context.TimeSlots
            .Where(ts => ts.StartTime <= arrivalTime && arrivalTime < ts.EndTime)
            .OrderBy(ts => ts.StartTime)
            .FirstOrDefaultAsync();

        if (existing != null)
            return existing;

        // No predefined slot covers this time � create a 2-hour fallback snapped to whole hours
        var start = TimeSpan.FromHours(arrivalTime.Hours);
        var end = start.Add(TimeSpan.FromHours(2));

        var timeSlot = new TimeSlot
        {
            StartTime = start,
            EndTime = end,
            Name = $@"{start:hh\:mm} - {end:hh\:mm}"
        };

        context.TimeSlots.Add(timeSlot);
        await context.SaveChangesAsync();

        return timeSlot;
    }
}