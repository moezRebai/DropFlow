using DropFlow.Application.Interfaces.Drivers;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Enums;
using DropFlow.Shared.Common;
using DropFlow.Shared.Drivers;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Application.Services.Drivers;

/// <summary>
/// Service dédié à l'application mobile livreur
/// 
/// Flux de résolution du livreur :
/// JWT (UserId claim) → ApplicationUser → Driver (via UserId) → RouteTeam → Route → Deliveries
/// </summary>
public class DriverAppService(
    IApplicationDbContext context,
    ITenantService tenantService,
    IAuditService auditService,
    IFileStorageService fileStorageService,
    ILogger<DriverAppService> logger)
    : IDriverAppService
{
    // ═══════════════════════════════════════════════════════════
    // GET TODAY'S ROUTE
    // ═══════════════════════════════════════════════════════════
    
    public async Task<DriverTodayResponse> GetTodayRouteAsync()
    {
        try
        {
            // 1. Résoudre le Driver depuis le User connecté
            var driver = await GetCurrentDriverAsync();
            if (driver == null)
            {
                return new DriverTodayResponse
                {
                    HasRoute = false,
                    Message = "Votre compte livreur n'est pas configuré. Contactez votre manager."
                };
            }

            // 2. Chercher la route du jour via RouteTeam
            var today = DateTime.UtcNow.Date;
            
            var route = await context.Routes
                .Include(r => r.Vehicle)
                .Include(r => r.Team)
                    .ThenInclude(t => t.Driver)
                    .ThenInclude(d => d.User)
                .Include(r => r.Deliveries)
                    .ThenInclude(d => d.Client)
                .Include(r => r.Deliveries)
                    .ThenInclude(d => d.ClientAddress)
                .Include(r => r.Deliveries)
                    .ThenInclude(d => d.TimeSlot)
                .Include(r => r.Deliveries)
                    .ThenInclude(d => d.Items)
                .Where(r => r.Date.Date == today)
                .Where(r => r.Status == RouteStatus.Confirmed || r.Status == RouteStatus.InProgress)
                .Where(r => r.Team.Any(t => t.DriverId == driver.Id))
                .OrderByDescending(r => r.Status) // InProgress d'abord, puis Confirmed
                .FirstOrDefaultAsync();

            if (route == null)
            {
                return new DriverTodayResponse
                {
                    HasRoute = false,
                    Message = "Aucune tournée prévue pour aujourd'hui."
                };
            }

            // 3. Mapper vers le DTO
            var dto = new DriverRouteDto
            {
                RouteId = route.Id,
                Reference = route.Reference,
                Date = route.Date,
                VehicleName = $"{route.Vehicle.Brand} {route.Vehicle.Model}",
                DepartureAddress = route.DepartureAddress ?? string.Empty,
                StartTime = route.StartTime,
                EstimatedEndTime = route.EstimatedEndTime,
                Status = route.Status,
                StatusDisplay = route.Status.Humanize(),
                TotalDeliveries = route.TotalDeliveries,
                TotalDistanceKm = route.TotalDistance,
                TotalDurationMinutes = route.TotalDuration,
                TeamMembers = route.Team
                    .Select(t => t.Driver.User.FullName)
                    .ToList(),
                Deliveries = route.Deliveries
                    .Where(d => d.SequenceOrder.HasValue)
                    .OrderBy(d => d.SequenceOrder)
                    .Select(d => new DriverDeliveryListDto
                    {
                        Id = d.Id,
                        SequenceOrder = d.SequenceOrder ?? 0,
                        Reference = d.Reference,
                        ClientName = d.Client.DisplayName,
                        City = d.ClientAddress.City,
                        ZipCode = d.ClientAddress.ZipCode,
                        TimeSlotName = d.TimeSlot?.Name,
                        EstimatedArrivalTime = d.EstimatedArrivalTime,
                        Status = d.Status,
                        StatusDisplay = d.Status.Humanize(),
                        WithAssembly = d.WithAssembly,
                        TotalPackages = d.Items.Sum(i => i.Quantity),
                        HasClientPayment = d.ClientPaymentAmount.HasValue && d.ClientPaymentAmount > 0,
                        IsClientAbsent = d.IsClientAbsent,
                        IsValidated = d.Status == DeliveryStatus.Delivered
                    })
                    .ToList()
            };

            return new DriverTodayResponse
            {
                HasRoute = true,
                Route = dto
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting today's route for driver");
            return new DriverTodayResponse
            {
                HasRoute = false,
                Message = "Erreur lors du chargement de la tournée."
            };
        }
    }

    // ═══════════════════════════════════════════════════════════
    // GET DELIVERY DETAIL
    // ═══════════════════════════════════════════════════════════
    
    public async Task<ResponseResult<DriverDeliveryDetailDto>> GetDeliveryDetailAsync(int deliveryId)
    {
        try
        {
            // 1. Résoudre le Driver
            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return ResponseResult<DriverDeliveryDetailDto>.Failure("Compte livreur non configuré");

            // 2. Charger la livraison
            var delivery = await context.Deliveries
                .Include(d => d.Client)
                .Include(d => d.ClientAddress)
                .Include(d => d.Store)
                .Include(d => d.TimeSlot)
                .Include(d => d.Items)
                .Include(d => d.Route)
                    .ThenInclude(r => r!.Team)
                .FirstOrDefaultAsync(d => d.Id == deliveryId);

            if (delivery == null)
                return ResponseResult<DriverDeliveryDetailDto>.Failure("Livraison introuvable");

            // 3. Vérifier que la livraison appartient à une route assignée au livreur
            if (delivery.Route == null || !delivery.Route.Team.Any(t => t.DriverId == driver.Id))
                return ResponseResult<DriverDeliveryDetailDto>.Failure("Accès non autorisé à cette livraison");

            // 4. Mapper (sans données confidentielles)
            var dto = new DriverDeliveryDetailDto
            {
                Id = delivery.Id,
                SequenceOrder = delivery.SequenceOrder ?? 0,
                Reference = delivery.Reference,
                
                // Client
                ClientFirstName = delivery.Client.FirstName,
                ClientLastName = delivery.Client.LastName,
                ClientName = delivery.Client.DisplayName,
                ClientPhone = delivery.Client.Phone,
                ClientEmail = delivery.Client.Email,
                
                // Adresse
                Address = delivery.ClientAddress.Address,
                ZipCode = delivery.ClientAddress.ZipCode,
                City = delivery.ClientAddress.City,
                AddressComplement = delivery.ClientAddress.Complement,
                FullAddress = delivery.ClientAddress.FullAddress,
                Latitude = delivery.ClientAddress.Latitude,
                Longitude = delivery.ClientAddress.Longitude,
                
                // Enseigne
                StoreName = delivery.Store.Name,
                FileNumber = delivery.FileNumber,
                
                // Créneau
                ScheduledDate = delivery.ScheduledDate,
                TimeSlotName = delivery.TimeSlot?.Name,
                EstimatedArrivalTime = delivery.EstimatedArrivalTime,
                
                // Prestation
                WithAssembly = delivery.WithAssembly,
                TotalPackages = delivery.Items.Sum(i => i.Quantity),
                ClientPaymentAmount = delivery.ClientPaymentAmount,
                
                // Notes chauffeur UNIQUEMENT (pas InternalNotes)
                DeliveryNotes = delivery.DeliveryNotes,
                
                // Produits
                Items = delivery.Items.Select(i => new DriverDeliveryItemDto
                {
                    Reference = i.Reference,
                    Designation = i.Designation,
                    Quantity = i.Quantity,
                    Information = i.Information
                }).ToList(),
                
                // Statut
                Status = delivery.Status,
                StatusDisplay = delivery.Status.Humanize(),
                
                // Validation
                IsValidated = delivery.Status == DeliveryStatus.Delivered,
                IsClientAbsent = delivery.IsClientAbsent,
                ValidationComment = delivery.ValidationComment,
                DeliveredDateTime = delivery.DeliveredDateTime,
                HasSignature = !string.IsNullOrEmpty(delivery.SignatureUrl),
                HasPhoto = !string.IsNullOrEmpty(delivery.PhotoUrl)
            };

            return ResponseResult<DriverDeliveryDetailDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting delivery detail {DeliveryId}", deliveryId);
            return ResponseResult<DriverDeliveryDetailDto>.Failure("Erreur lors du chargement de la livraison");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDATE DELIVERY
    // ═══════════════════════════════════════════════════════════
    
    public async Task<ResponseResult<bool>> ValidateDeliveryAsync(int deliveryId, ValidateDeliveryDto dto)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        
        try
        {
            // 1. Résoudre le Driver
            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return ResponseResult<bool>.Failure("Compte livreur non configuré");

            var tenantId = tenantService.GetTenantId();

            // 2. Charger la livraison
            var delivery = await context.Deliveries
                .Include(d => d.Route)
                    .ThenInclude(r => r!.Team)
                .FirstOrDefaultAsync(d => d.Id == deliveryId);

            if (delivery == null)
                return ResponseResult<bool>.Failure("Livraison introuvable");

            // 3. Vérifier accès
            if (delivery.Route == null || !delivery.Route.Team.Any(t => t.DriverId == driver.Id))
                return ResponseResult<bool>.Failure("Accès non autorisé");

            // 4. Vérifier que la livraison est validable
            if (delivery.Status == DeliveryStatus.Delivered)
                return ResponseResult<bool>.Failure("Cette livraison est déjà validée");
            
            if (delivery.Status == DeliveryStatus.Canceled)
                return ResponseResult<bool>.Failure("Impossible de valider une livraison annulée");

            // 5. Valider la signature (obligatoire sauf client absent)
            if (!dto.IsClientAbsent && string.IsNullOrWhiteSpace(dto.SignatureBase64))
                return ResponseResult<bool>.Failure("La signature est obligatoire (sauf client absent)");

            // 6. Sauvegarder la signature si fournie
            if (!string.IsNullOrWhiteSpace(dto.SignatureBase64))
            {
                var signaturePath = await fileStorageService.SaveFileAsync(
                    tenantId, deliveryId, dto.SignatureBase64, "signature", "png");
                delivery.SignatureUrl = signaturePath;
            }

            // 7. Sauvegarder la photo si fournie
            if (!string.IsNullOrWhiteSpace(dto.PhotoBase64))
            {
                var photoPath = await fileStorageService.SaveFileAsync(
                    tenantId, deliveryId, dto.PhotoBase64, "photo", "jpg");
                delivery.PhotoUrl = photoPath;
            }

            // 8. Mettre à jour la livraison
            delivery.Status = DeliveryStatus.Delivered;
            delivery.DeliveredDateTime = DateTime.UtcNow;
            delivery.ValidationComment = dto.Comment;
            delivery.IsClientAbsent = dto.IsClientAbsent;
            delivery.ValidatedByDriverId = driver.Id;
            delivery.ActualArrivalTime = DateTime.UtcNow.TimeOfDay;
            delivery.ModifiedDate = DateTime.UtcNow;
            delivery.ModifiedBy = tenantService.GetCurrentUser()?.Id ?? "system";

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            // 9. Audit
            await auditService.LogAsync(
                tenantId,
                driver.Id.ToString(),
                "DeliveryValidated",
                "Delivery",
                delivery.Id,
                $"Livraison {delivery.Reference} validée par livreur {driver.User.FullName}" +
                (dto.IsClientAbsent ? " (client absent)" : ""));

            logger.LogInformation(
                "Delivery {DeliveryId} validated by driver {DriverId} (ClientAbsent={IsAbsent})",
                deliveryId, driver.Id, dto.IsClientAbsent);

            return ResponseResult<bool>.Success(true);
        }
        catch (ArgumentException ex)
        {
            await transaction.RollbackAsync();
            logger.LogWarning(ex, "Validation error for delivery {DeliveryId}", deliveryId);
            return ResponseResult<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error validating delivery {DeliveryId}", deliveryId);
            return ResponseResult<bool>.Failure("Erreur lors de la validation de la livraison");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // START ROUTE
    // ═══════════════════════════════════════════════════════════
    
    public async Task<ResponseResult<bool>> StartRouteAsync(int routeId)
    {
        try
        {
            var tenantId = tenantService.GetTenantId();

            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return ResponseResult<bool>.Failure("Compte livreur non configuré");

            var route = await context.Routes
                .Include(r => r.Team)
                .Include(r => r.Deliveries)
                .FirstOrDefaultAsync(r => r.Id == routeId);

            if (route == null)
                return ResponseResult<bool>.Failure("Tournée introuvable");

            // Vérifier que le livreur est assigné à cette route
            if (route.Team.All(t => t.DriverId != driver.Id))
                return ResponseResult<bool>.Failure("Accès non autorisé");

            if (route.Status != RouteStatus.Confirmed)
                return ResponseResult<bool>.Failure("Seule une tournée confirmée peut être démarrée");

            // Démarrer la route
            route.Start();

            // Passer toutes les livraisons Confirmed en InProgress
            foreach (var delivery in route.Deliveries.Where(d => d.Status == DeliveryStatus.Confirmed))
            {
                delivery.Status = DeliveryStatus.InProgress;
                delivery.ModifiedDate = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();

            await auditService.LogAsync(
                tenantId,
                driver.Id.ToString(),
                "RouteStarted",
                "Route",
                route.Id,
                $"Tournée {route.Reference} démarrée par livreur {driver.User.FullName}");

            logger.LogInformation("Route {RouteId} started by driver {DriverId}", routeId, driver.Id);

            return ResponseResult<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting route {RouteId}", routeId);
            return ResponseResult<bool>.Failure("Erreur lors du démarrage de la tournée");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // COMPLETE ROUTE
    // ═══════════════════════════════════════════════════════════
    
    public async Task<ResponseResult<bool>> CompleteRouteAsync(int routeId)
    {
        try
        {
            var tenantId = tenantService.GetTenantId();

            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return ResponseResult<bool>.Failure("Compte livreur non configuré");

            var route = await context.Routes
                .Include(r => r.Team)
                .Include(r => r.Deliveries)
                .FirstOrDefaultAsync(r => r.Id == routeId);

            if (route == null)
                return ResponseResult<bool>.Failure("Tournée introuvable");

            if (!route.Team.Any(t => t.DriverId == driver.Id))
                return ResponseResult<bool>.Failure("Accès non autorisé");

            if (route.Status != RouteStatus.InProgress)
                return ResponseResult<bool>.Failure("Seule une tournée en cours peut être terminée");

            // Terminer la route
            route.Complete();
            await context.SaveChangesAsync();

            // Compter les stats
            var deliveredCount = route.Deliveries.Count(d => d.Status == DeliveryStatus.Delivered);
            var totalCount = route.Deliveries.Count;

            await auditService.LogAsync(
                tenantId,
                driver.Id.ToString(),
                "RouteCompleted",
                "Route",
                route.Id,
                $"Tournée {route.Reference} terminée par {driver.User.FullName} ({deliveredCount}/{totalCount} livrées)");

            logger.LogInformation(
                "Route {RouteId} completed by driver {DriverId} ({Delivered}/{Total} delivered)",
                routeId, driver.Id, deliveredCount, totalCount);

            return ResponseResult<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            return ResponseResult<bool>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error completing route {RouteId}", routeId);
            return ResponseResult<bool>.Failure("Erreur lors de la fin de la tournée");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Résout le Driver depuis le User connecté (JWT)
    /// User.Id → Driver.UserId
    /// </summary>
    private async Task<Domain.Entities.Driver?> GetCurrentDriverAsync()
    {
        var currentUser = tenantService.GetCurrentUser();
        if (currentUser == null) return null;

        return await context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == currentUser.Id && d.IsActive);
    }
}
