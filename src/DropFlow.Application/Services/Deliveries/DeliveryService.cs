using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Deliveries;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Entities;
using DropFlow.Domain.Enums;
using DropFlow.Shared.Common;
using DropFlow.Shared.Deliveries;
using DropFlow.Shared.Routes;
using DropFlow.Shared.TimeSlots;
using FluentValidation;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Application.Services.Deliveries;

public class DeliveryService(
    IApplicationDbContext context,
    ITenantService tenantService,
    IAuditService auditService,
    IDeliveryReferenceService referenceService,
    IGeocodingService geocodingService,
    IDriverAvailabilityService driverAvailabilityService,
    IValidator<CreateDeliveryDto> createValidator,
    IValidator<UpdateDeliveryDto> updateValidator,
    ILogger<DeliveryService> logger)
    : IDeliveryService
{
    public async Task<ResponseResult<DeliveryDto>> GetDeliveryByIdAsync(int id)
    {
        try
        {
            var delivery = await context.Deliveries
                .Include(d => d.Client)
                .Include(d => d.ClientAddress)
                .Include(d => d.Store)
                .Include(d => d.Route) // ✅ AJOUTÉ
                .Include(d => d.TimeSlot)
                .Include(d => d.Items)
                .Include(d => d.UrgentDriver) // ✅ AJOUTÉ
                .ThenInclude(ud => ud.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (delivery == null)
            {
                return ResponseResult<DeliveryDto>.Failure("Livraison introuvable");
            }

            var dto = new DeliveryDto
            {
                Id = delivery.Id,
                SequentialNumber = delivery.SequentialNumber,
                Reference = delivery.Reference,
                Type = delivery.Type, // ✅ AJOUTÉ
                TypeDisplay = delivery.Type.Humanize(), // ✅ AJOUTÉ

                // Client
                ClientId = delivery.ClientId,
                ClientName = delivery.Client.DisplayName,
                ClientPhone = delivery.Client.Phone,
                ClientEmail = delivery.Client.Email,
                Client = new ClientDetailDto
                {
                    Id = delivery.Client.Id,
                    FirstName = delivery.Client.FirstName,
                    LastName = delivery.Client.LastName,
                    Phone = delivery.Client.Phone,
                    Email = delivery.Client.Email
                },

                // Address
                ClientAddressId = delivery.ClientAddressId,
                Address = delivery.ClientAddress.Address,
                ZipCode = delivery.ClientAddress.ZipCode,
                City = delivery.ClientAddress.City,
                AddressComplement = delivery.ClientAddress.Complement,
                AddressLabel = delivery.ClientAddress.Label,

                // Store
                StoreId = delivery.StoreId,
                StoreName = delivery.Store.Name,

                // Details
                FileNumber = delivery.FileNumber,
                ScheduledDate = delivery.ScheduledDate,
                Price = delivery.Price,
                ClientPaymentAmount = delivery.ClientPaymentAmount,
                StorePaymentAmount = delivery.StorePaymentAmount,

                // Organization
                Status = delivery.Status,
                StatusDisplay = delivery.Status.Humanize(),
                RouteId = delivery.RouteId, // ✅ AJOUTÉ
                RouteReference = delivery.Route?.Reference, // ✅ AJOUTÉ
                EstimatedArrivalTime = delivery.EstimatedArrivalTime, // ✅ AJOUTÉ
                ActualArrivalTime = delivery.ActualArrivalTime, // ✅ AJOUTÉ
                WithAssembly = delivery.WithAssembly,
                DeliveryNotes = delivery.DeliveryNotes,
                InternalNotes = delivery.InternalNotes,
                EstimatedDurationMinutes = delivery.EstimatedDurationMinutes,
                TimeSlotId = delivery.TimeSlotId,
                Latitude = delivery.ClientAddress.Latitude,
                Longitude = delivery.ClientAddress.Longitude,
                // Urgent
                UrgentDriverId = delivery.UrgentDriverId, // ✅ AJOUTÉ
                UrgentDriverName = delivery.UrgentDriver?.User.FullName, // ✅ AJOUTÉ

                // Items
                Items = delivery.Items.Select(i => new DeliveryItemDto
                {
                    Id = i.Id,
                    Reference = i.Reference,
                    Designation = i.Designation,
                    Quantity = i.Quantity,
                    Information = i.Information
                }).ToList(),
                TotalPackages = delivery.Items.Sum(i => i.Quantity),

                // Audit
                CreatedDate = delivery.CreatedDate,
                CreatedBy = delivery.CreatedBy
            };

            // Resolve creator name from userId
            var creator = await context.Users.FindAsync(delivery.CreatedBy);
            if (creator != null)
                dto.CreatedBy = creator.FullName;

            if (delivery.TimeSlot != null)
                dto.TimeSlot = new TimeSlotDto
                {
                    Id = delivery.TimeSlot.Id,
                    Name = delivery.TimeSlot.Name,
                    StartTime = delivery.TimeSlot.StartTime,
                    EndTime = delivery.TimeSlot.EndTime,
                    DisplayOrder = delivery.TimeSlot.DisplayOrder,
                };

            return ResponseResult<DeliveryDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading delivery {DeliveryId}", id);
            return ResponseResult<DeliveryDto>.Failure($"Erreur lors du chargement : {ex.Message}");
        }
    }

    public async Task<ResponseResult<List<DeliveryDto>>> GetUnassignedDeliveriesAsync(DateTime date)
    {
        try
        {
            var result = await context.Deliveries
                .Include(d => d.Client)
                .Include(d => d.ClientAddress)
                .Include(d => d.Store)
                .Include(d => d.Route) // ✅ AJOUTÉ
                .Include(d => d.TimeSlot)
                .Include(d => d.Items)
                .Include(d => d.UrgentDriver) // ✅ AJOUTÉ
                .ThenInclude(ud => ud.User)
                .Where(d =>
                    d.Type == DeliveryType.Standard &&
                    (d.RouteId == null || d.RouteId != null && d.Route.Status == RouteStatus.Draft)
                    && d.ScheduledDate.HasValue &&
                    d.ScheduledDate.Value.Date == date.Date &&
                    d.Status == DeliveryStatus.Confirmed)
                .Select(delivery => new DeliveryDto
                {
                    Id = delivery.Id,
                    SequentialNumber = delivery.SequentialNumber,
                    Reference = delivery.Reference,
                    Type = delivery.Type, // ✅ AJOUTÉ
                    TypeDisplay = delivery.Type.Humanize(), // ✅ AJOUTÉ

                    // Client
                    ClientId = delivery.ClientId,
                    ClientName = delivery.Client.DisplayName,
                    ClientPhone = delivery.Client.Phone,
                    ClientEmail = delivery.Client.Email,
                    Client = new ClientDetailDto
                    {
                        Id = delivery.Client.Id,
                        FirstName = delivery.Client.FirstName,
                        LastName = delivery.Client.LastName,
                        Phone = delivery.Client.Phone,
                        Email = delivery.Client.Email
                    },

                    // Address
                    ClientAddressId = delivery.ClientAddressId,
                    Address = delivery.ClientAddress.Address,
                    ZipCode = delivery.ClientAddress.ZipCode,
                    City = delivery.ClientAddress.City,
                    AddressComplement = delivery.ClientAddress.Complement,
                    AddressLabel = delivery.ClientAddress.Label,

                    // Store
                    StoreId = delivery.StoreId,
                    StoreName = delivery.Store.Name,

                    // Details
                    FileNumber = delivery.FileNumber,
                    ScheduledDate = delivery.ScheduledDate,
                    Price = delivery.Price,
                    ClientPaymentAmount = delivery.ClientPaymentAmount,
                    StorePaymentAmount = delivery.StorePaymentAmount,

                    // Organization
                    Status = delivery.Status,
                    StatusDisplay = delivery.Status.Humanize(),
                    RouteId = delivery.RouteId, // ✅ AJOUTÉ
                    RouteReference = delivery.Route.Reference, // ✅ AJOUTÉ
                    EstimatedArrivalTime = delivery.EstimatedArrivalTime, // ✅ AJOUTÉ
                    ActualArrivalTime = delivery.ActualArrivalTime, // ✅ AJOUTÉ
                    WithAssembly = delivery.WithAssembly,
                    DeliveryNotes = delivery.DeliveryNotes,
                    InternalNotes = delivery.InternalNotes,
                    EstimatedDurationMinutes = delivery.EstimatedDurationMinutes,
                    TimeSlotId = delivery.TimeSlotId,
                    Latitude = delivery.ClientAddress.Latitude,
                    Longitude = delivery.ClientAddress.Longitude,
                    
                    // Urgent
                    UrgentDriverId = delivery.UrgentDriverId, // ✅ AJOUTÉ
                    UrgentDriverName = delivery.UrgentDriver != null ? delivery.UrgentDriver.User.FullName : null,

                    // Items
                    Items = delivery.Items.Select(i => new DeliveryItemDto
                    {
                        Id = i.Id,
                        Reference = i.Reference,
                        Designation = i.Designation,
                        Quantity = i.Quantity,
                        Information = i.Information
                    }).ToList(),
                    TotalPackages = delivery.Items.Sum(i => i.Quantity),

                    // Audit
                    CreatedDate = delivery.CreatedDate,
                    CreatedBy = delivery.CreatedBy
                })
                .OrderBy(d => d.City)
                .ToListAsync();

            return ResponseResult<List<DeliveryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading available deliveries");
            return ResponseResult<List<DeliveryDto>>.Failure($"Erreur lors du chargement : {ex.Message}");
        }
    }

    public async Task<PagedResult<DeliveryViewDto>> GetDeliveriesAsync(DeliveryFilterDto filter)
    {
        try
        {
            var query = context.Deliveries
                .AsNoTracking()
                .Include(d => d.Client)
                .Include(d => d.ClientAddress)
                .Include(d => d.Store)
                .Include(d => d.Route)
                .Include(d => d.TimeSlot)
                .Include(d => d.UrgentDriver)
                .ThenInclude(ud => ud.User)
                .AsQueryable();

            // Filtres
            if (filter.StoreId.HasValue)
                query = query.Where(d => d.StoreId == filter.StoreId.Value);

            // ✅ AJOUTÉ
            if (filter.Type.HasValue)
                query = query.Where(d => d.Type == filter.Type.Value);

            if (filter.Statuses != null && filter.Statuses.Any())
                query = query.Where(d => filter.Statuses.Contains(d.Status));

            if (!string.IsNullOrEmpty(filter.ClientSearch))
            {
                var clientSearch = filter.ClientSearch.ToLower();
                query = query.Where(d =>
                    d.Client.FirstName.ToLower().Contains(clientSearch) ||
                    d.Client.LastName.ToLower().Contains(clientSearch));
            }

            if (filter.DateFrom.HasValue)
                query = query.Where(d => d.ScheduledDate >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(d => d.ScheduledDate <= filter.DateTo.Value);

            // ✅ AJOUTÉ
            if (filter.RouteId.HasValue)
                query = query.Where(d => d.RouteId == filter.RouteId.Value);

            // ✅ AJOUTÉ
            if (filter.UrgentDriverId.HasValue)
                query = query.Where(d => d.UrgentDriverId == filter.UrgentDriverId.Value);

            if (filter.WithAssembly.HasValue)
                query = query.Where(d => d.WithAssembly == filter.WithAssembly.Value);

            if (!string.IsNullOrEmpty(filter.GlobalSearch))
            {
                var gs = filter.GlobalSearch.ToLower();
                query = query.Where(d =>
                    d.Reference.ToLower().Contains(gs) ||
                    d.SequentialNumber.ToString().Contains(gs) ||
                    d.Client.FirstName.ToLower().Contains(gs) ||
                    d.Client.LastName.ToLower().Contains(gs) ||
                    d.ClientAddress.City.ToLower().Contains(gs));
            }

            var totalCount = await query.CountAsync();

            // Tri
            query = ApplySorting(query, filter);

            // Pagination
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(d => new DeliveryViewDto
                {
                    Id = d.Id,
                    CreatedDate = d.CreatedDate,
                    SequentialNumber = d.SequentialNumber,
                    Reference = d.Reference,
                    Type = d.Type, // ✅ AJOUTÉ
                    ClientName = d.Client.DisplayName,
                    City = d.ClientAddress.City,
                    FullAddress = d.ClientAddress.FullAddress,
                    StoreName = d.Store.Name,
                    Price = d.Price,
                    ScheduledDate = d.ScheduledDate,
                    Status = d.Status,
                    RouteId = d.RouteId, // ✅ AJOUTÉ
                    RouteReference = d.Route != null ? d.Route.Reference : null, // ✅ AJOUTÉ
                    UrgentDriverName = d.UrgentDriver != null ? d.UrgentDriver.User.FullName : null, // ✅ AJOUTÉ
                    WithAssembly = d.WithAssembly,
                    TotalPackages = d.Items.Sum(i => (int)i.Quantity)
                })
                .ToListAsync();

            return new PagedResult<DeliveryViewDto>
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
            logger.LogError(ex, "Error loading deliveries");
            return new PagedResult<DeliveryViewDto>
            {
                Items = [],
                TotalCount = 0,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = 0
            };
        }
    }

    public async Task<ResponseResult<int>> CreateDeliveryAsync(CreateDeliveryDto dto)
    {
        try
        {
            var tenantId = tenantService.GetTenantId();
            var currentUser = await tenantService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return ResponseResult<int>.Failure("User not found");
            }

            var validationResult = await createValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ResponseResult<int>.Failure(errors);
            }

            // ✅ AJOUTÉ : Validation Type-specific
            if (dto.Type == DeliveryType.Urgent)
            {
                if (!dto.UrgentDriverId.HasValue)
                    return ResponseResult<int>.Failure("Un livreur est requis pour une livraison urgente");

                if (!dto.TimeSlotId.HasValue)
                    return ResponseResult<int>.Failure("Un créneau est requis pour une livraison urgente");

                if (!dto.ScheduledDate.HasValue)
                    return ResponseResult<int>.Failure("Une date est requise pour une livraison urgente");

                // Check driver exists and active
                var driver = await context.Drivers.FindAsync(dto.UrgentDriverId.Value);
                if (driver is not { IsActive: true })
                    return ResponseResult<int>.Failure("Livreur introuvable ou inactif");

                // Check availability (warning only, allow override)
                var availability = await driverAvailabilityService.CheckAvailabilityAsync(
                    dto.UrgentDriverId.Value,
                    dto.ScheduledDate.Value);

                if (!availability.IsAvailable)
                {
                    logger.LogWarning(
                        "Urgent delivery assigned to busy driver {DriverId}: {Reason}",
                        dto.UrgentDriverId.Value,
                        availability.ConflictReason);
                }
            }

            if (dto is { Type: DeliveryType.Standard, UrgentDriverId: not null })
                return ResponseResult<int>.Failure("Une livraison standard ne peut pas avoir de livreur urgent");

            // Get or create client
            var clientId = await GetOrCreateClientAsync(dto, tenantId, currentUser.Id);

            // Get or create address
            var addressId = await GetOrCreateAddressAsync(dto, clientId);

            // Generate reference
            var reference = await referenceService.GenerateReferenceAsync(tenantId);
            var sequentialNumber = await referenceService.GetNextSequentialNumberAsync(tenantId);

            // Create delivery
            var delivery = new Delivery
            {
                TenantId = tenantId,
                SequentialNumber = sequentialNumber,
                Reference = reference,
                Type = dto.Type, // ✅ AJOUTÉ
                Status = DeliveryStatus.ToBePlanned,
                ClientId = clientId,
                ClientAddressId = addressId,
                StoreId = dto.StoreId,
                FileNumber = dto.FileNumber,
                ScheduledDate = dto.ScheduledDate,
                TimeSlotId = dto.TimeSlotId,
                Price = dto.Price,
                ClientPaymentAmount = dto.ClientPaymentAmount,
                StorePaymentAmount = dto.StorePaymentAmount,
                WithAssembly = dto.WithAssembly,
                EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
                DeliveryNotes = dto.DeliveryNotes,
                InternalNotes = dto.InternalNotes,
                UrgentDriverId = dto.UrgentDriverId, // ✅ AJOUTÉ
                CreatedDate = DateTime.UtcNow,
                CreatedBy = currentUser.Id,
                ModifiedDate = DateTime.UtcNow,
                ModifiedBy = currentUser.Id
            };

            context.Deliveries.Add(delivery);
            await context.SaveChangesAsync();

            // Add items
            if (dto.Items.Count != 0)
            {
                foreach (var itemDto in dto.Items)
                {
                    var item = new DeliveryItem
                    {
                        DeliveryId = delivery.Id,
                        Reference = itemDto.Reference,
                        Designation = itemDto.Designation,
                        Quantity = itemDto.Quantity,
                        Information = itemDto.Information
                    };
                    context.DeliveryItems.Add(item);
                }

                await context.SaveChangesAsync();
            }

            // Audit
            await auditService.LogAsync(
                tenantId: tenantId,
                userId: currentUser.Id,
                action: "DeliveryCreated",
                entityName: nameof(Delivery),
                entityId: delivery.Id,
                changes: new { delivery.Reference, dto.StoreId, dto.Price },
                severity: AuditSeverity.Info
            );

            logger.LogInformation("Delivery created: {DeliveryId} - {Reference}", delivery.Id, delivery.Reference);

            return ResponseResult<int>.Success(delivery.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating delivery");
            return ResponseResult<int>.Failure($"Erreur lors de la création : {ex.Message}");
        }
    }

    public async Task<ResponseResult> UpdateDeliveryAsync(int id, UpdateDeliveryDto dto)
    {
        try
        {
            var currentUser = await tenantService.GetCurrentUserAsync();

            var validationResult = await updateValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return ResponseResult.Failure(errors);
            }

            var delivery = await context.Deliveries
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (delivery == null)
                return ResponseResult.Failure("Livraison introuvable");

            // ✅ AJOUTÉ : Cannot update if in route sheet (except draft)
            if (delivery.RouteId.HasValue)
            {
                var routeSheet = await context.Routes.FindAsync(delivery.RouteId.Value);
                if (routeSheet?.Status != RouteStatus.Draft)
                    return ResponseResult.Failure("Impossible de modifier une livraison dans une tournée confirmée");
            }

            // ✅ AJOUTÉ : Type-specific validation
            if (dto.Type == DeliveryType.Urgent)
            {
                if (!dto.UrgentDriverId.HasValue)
                    return ResponseResult.Failure("Un livreur est requis pour une livraison urgente");

                if (!dto.TimeSlotId.HasValue)
                    return ResponseResult.Failure("Un créneau est requis pour une livraison urgente");
            }

            if (dto is { Type: DeliveryType.Standard, UrgentDriverId: not null })
                return ResponseResult.Failure("Une livraison standard ne peut pas avoir de livreur urgent");

            var oldDelivery = new
            {
                delivery.Type, // ✅ AJOUTÉ
                delivery.ClientAddressId,
                delivery.StoreId,
                delivery.FileNumber,
                delivery.ScheduledDate,
                delivery.TimeSlotId,
                delivery.Price,
                delivery.ClientPaymentAmount,
                delivery.StorePaymentAmount,
                delivery.WithAssembly,
                delivery.EstimatedDurationMinutes,
                delivery.DeliveryNotes,
                delivery.InternalNotes,
                delivery.UrgentDriverId // ✅ AJOUTÉ
            };

            // Handle Client change
            if (dto.ClientId != delivery.ClientId)
            {
                delivery.ClientId =
                    dto.ClientId ?? await GetOrCreateClientAsync(dto, delivery.TenantId, currentUser.Id);
            }

            // Handle Address change
            if (dto.ClientAddressId != delivery.ClientAddressId)
            {
                delivery.ClientAddressId = dto.ClientAddressId ?? await GetOrCreateAddressAsync(dto, delivery.ClientId);
            }

            // Update fields
            delivery.Type = dto.Type;
            delivery.Status = dto.Status;
            delivery.StoreId = dto.StoreId;
            delivery.FileNumber = dto.FileNumber;
            delivery.ScheduledDate = dto.ScheduledDate;
            delivery.TimeSlotId = dto.TimeSlotId;
            delivery.Price = dto.Price;
            delivery.ClientPaymentAmount = dto.ClientPaymentAmount;
            delivery.StorePaymentAmount = dto.StorePaymentAmount;
            delivery.WithAssembly = dto.WithAssembly;
            delivery.EstimatedDurationMinutes = dto.EstimatedDurationMinutes;
            delivery.DeliveryNotes = dto.DeliveryNotes;
            delivery.InternalNotes = dto.InternalNotes;
            delivery.UrgentDriverId = dto.UrgentDriverId; // ✅ AJOUTÉ
            delivery.ModifiedDate = DateTime.UtcNow;
            delivery.ModifiedBy = currentUser?.Id;

            // Update items
            if (dto.Items != null)
            {
                await UpdateDeliveryItemsAsync(delivery, dto.Items);
            }

            await context.SaveChangesAsync();

            // Audit
            await auditService.LogAsync(
                tenantId: delivery.TenantId,
                userId: currentUser.Id,
                action: "DeliveryUpdated",
                entityName: nameof(Delivery),
                entityId: delivery.Id,
                changes: new { Old = oldDelivery, New = new { dto.StoreId, dto.Price, dto.Status } },
                severity: AuditSeverity.Info
            );

            logger.LogInformation("Delivery updated: {DeliveryId}", id);

            return ResponseResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating delivery {DeliveryId}", id);
            return ResponseResult.Failure($"Erreur lors de la mise à jour : {ex.Message}");
        }
    }

    public async Task<ResponseResult> DeleteDeliveryAsync(int id)
    {
        try
        {
            var currentUser = await tenantService.GetCurrentUserAsync();
            var delivery = await context.Deliveries.FirstOrDefaultAsync(d => d.Id == id);

            if (delivery == null)
                return ResponseResult.Failure("Livraison introuvable");

            if (delivery.Status == DeliveryStatus.Delivered)
                return ResponseResult.Failure("Impossible de supprimer une livraison déjà effectuée");

            // ✅ AJOUTÉ
            if (delivery.RouteId.HasValue)
                return ResponseResult.Failure("Impossible de supprimer une livraison dans une tournée");

            var reference = delivery.Reference;

            context.Deliveries.Remove(delivery);
            await context.SaveChangesAsync();

            // Audit
            await auditService.LogAsync(
                tenantId: delivery.TenantId,
                userId: currentUser.Id,
                action: "DeliveryDeleted",
                entityName: nameof(Delivery),
                entityId: id,
                changes: new { reference },
                severity: AuditSeverity.Warning
            );

            logger.LogInformation("Livraison {Reference} supprimée par {UserId}", reference, currentUser.Id);

            return ResponseResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting delivery {DeliveryId}", id);
            return ResponseResult.Failure($"Erreur lors de la suppression : {ex.Message}");
        }
    }

    public async Task<ResponseResult> UpdateStatusAsync(int id, DeliveryStatus status)
    {
        try
        {
            var currentUser = await tenantService.GetCurrentUserAsync();
            var delivery = await context.Deliveries.FirstOrDefaultAsync(d => d.Id == id);

            if (delivery == null)
                return ResponseResult.Failure("Livraison introuvable");

            var oldStatus = delivery.Status;

            delivery.Status = status;
            delivery.ModifiedDate = DateTime.UtcNow;
            delivery.ModifiedBy = currentUser.Id;

            // ✅ AJOUTÉ : Auto-fill delivery timestamps
            if (status == DeliveryStatus.Delivered)
            {
                delivery.DeliveredDateTime = DateTime.UtcNow;
                if (!delivery.ActualArrivalTime.HasValue)
                    delivery.ActualArrivalTime = DateTime.UtcNow.TimeOfDay;
            }

            await context.SaveChangesAsync();

            await auditService.LogAsync(
                tenantId: delivery.TenantId,
                userId: currentUser.Id,
                action: "DeliveryStatusChanged",
                entityName: nameof(Delivery),
                entityId: delivery.Id,
                changes: new { OldStatus = oldStatus, NewStatus = status },
                severity: AuditSeverity.Info
            );

            return ResponseResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la mise à jour du statut");
            return ResponseResult.Failure("Erreur lors de la mise à jour du statut");
        }
    }

    public async Task<DeliveryStatsDto> GetStatsAsync()
    {
        var groups = await context.Deliveries
            .GroupBy(d => d.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(d => d.Price),
                TotalClientPayment = g.Sum(d => d.ClientPaymentAmount ?? 0),
                TotalStorePayment = g.Sum(d => d.StorePaymentAmount ?? 0)
            })
            .ToListAsync();

        return new DeliveryStatsDto
        {
            ToBePlannedCount = groups.FirstOrDefault(g => g.Status == DeliveryStatus.ToBePlanned)?.Count ?? 0,
            PlannedCount = groups.FirstOrDefault(g => g.Status == DeliveryStatus.Confirmed)?.Count ?? 0,
            InProgressCount = groups.FirstOrDefault(g => g.Status == DeliveryStatus.InProgress)?.Count ?? 0,
            DeliveredCount = groups.FirstOrDefault(g => g.Status == DeliveryStatus.Delivered)?.Count ?? 0,
            TotalAmount = groups.Sum(g => g.TotalAmount),
            TotalClientPayment = groups.Sum(g => g.TotalClientPayment),
            TotalStorePayment = groups.Sum(g => g.TotalStorePayment)
        };
    }

    public async Task<ResponseResult> DuplicateDeliveryAsync(int id)
    {
        try
        {
            var tenantId = tenantService.GetTenantId();
            var currentUser = await tenantService.GetCurrentUserAsync();

            var original = await context.Deliveries
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (original == null)
                return ResponseResult<int>.Failure("Livraison introuvable");

            var reference = await referenceService.GenerateReferenceAsync(tenantId);
            var sequentialNumber = await referenceService.GetNextSequentialNumberAsync(tenantId);

            var duplicate = new Delivery
            {
                TenantId = tenantId,
                SequentialNumber = sequentialNumber,
                Reference = reference,

                Type = original.Type, // ✅ AJOUTÉ
                Status = DeliveryStatus.ToBePlanned,
                ClientId = original.ClientId,
                ClientAddressId = original.ClientAddressId,
                StoreId = original.StoreId,
                FileNumber = original.FileNumber,
                ScheduledDate = null,
                TimeSlotId = original.TimeSlotId,
                Price = original.Price,
                ClientPaymentAmount = original.ClientPaymentAmount,
                StorePaymentAmount = original.StorePaymentAmount,
                WithAssembly = original.WithAssembly,
                EstimatedDurationMinutes = original.EstimatedDurationMinutes,
                DeliveryNotes = original.DeliveryNotes,
                InternalNotes = original.InternalNotes,

                Items = original.Items.Select(i => new DeliveryItem
                {
                    Reference = i.Reference,
                    Designation = i.Designation,
                    Quantity = i.Quantity,
                    Information = i.Information
                }).ToList(),

                CreatedDate = DateTime.UtcNow,
                CreatedBy = currentUser.Id,
                ModifiedDate = DateTime.UtcNow,
                ModifiedBy = currentUser.Id
            };

            context.Deliveries.Add(duplicate);
            await context.SaveChangesAsync();

            await auditService.LogAsync(
                tenantId: tenantId,
                userId: currentUser.Id,
                action: "DeliveryDuplicated",
                entityName: nameof(Delivery),
                entityId: duplicate.Id,
                changes: new { OriginalReference = original.Reference, NewReference = reference },
                severity: AuditSeverity.Info
            );

            logger.LogInformation("Livraison {Original} dupliquée vers {New} par {UserId}",
                original.Reference, reference, currentUser.Id);

            return ResponseResult.Success("Livraison dupliquée avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error duplicating delivery {DeliveryId}", id);
            return ResponseResult<int>.Failure($"Erreur lors de la duplication : {ex.Message}");
        }
    }

    public async Task<ResponseResult> BulkUpdateStatusAsync(List<int> deliveryIds, DeliveryStatus status)
    {
        try
        {
            var currentUser = await tenantService.GetCurrentUserAsync();
            var deliveries = await context.Deliveries
                .Where(d => deliveryIds.Contains(d.Id))
                .ToListAsync();

            foreach (var delivery in deliveries)
            {
                delivery.Status = status;
                delivery.ModifiedDate = DateTime.UtcNow;
                delivery.ModifiedBy = currentUser.Id;

                if (status == DeliveryStatus.Delivered)
                {
                    delivery.DeliveredDateTime = DateTime.UtcNow;
                    if (!delivery.ActualArrivalTime.HasValue)
                        delivery.ActualArrivalTime = DateTime.UtcNow.TimeOfDay;
                }
            }

            await context.SaveChangesAsync();

            return ResponseResult.Success($"{deliveries.Count} livraison(s) mise(s) à jour");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk updating status");
            return ResponseResult.Failure("Erreur lors de la mise à jour en masse");
        }
    }

    public async Task<ResponseResult> BulkDeleteAsync(List<int> deliveryIds)
    {
        try
        {
            var currentUser = await tenantService.GetCurrentUserAsync();
            var deliveries = await context.Deliveries
                .Where(d => deliveryIds.Contains(d.Id))
                .ToListAsync();

            var cannotDelete = deliveries.Where(d =>
                d.Status == DeliveryStatus.Delivered || d.RouteId.HasValue).ToList(); // ✅ MODIFIÉ

            if (cannotDelete.Any())
                return ResponseResult.Failure(
                    $"{cannotDelete.Count} livraison(s) ne peut/peuvent pas être supprimée(s)");

            foreach (var delivery in deliveries.Except(cannotDelete))
            {
                delivery.Status = DeliveryStatus.Canceled;
                delivery.ModifiedDate = DateTime.UtcNow;
                delivery.ModifiedBy = currentUser.Id;
            }

            await context.SaveChangesAsync();

            return ResponseResult.Success($"{deliveries.Count - cannotDelete.Count} livraison(s) supprimée(s)");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk deleting");
            return ResponseResult.Failure("Erreur lors de la suppression en masse");
        }
    }
    
    public async Task<bool> IsDeliveryAvailableForRouteAsync(int deliveryId, int? excludeRouteId = null)
    {
        try
        {
            var delivery = await context.Deliveries
                .Include(d => d.Route)
                .FirstOrDefaultAsync(d => d.Id == deliveryId);

            if (delivery == null)
                return false;

            // Si pas de route assignée, disponible
            if (delivery.RouteId == null)
                return true;

            // Si c'est la route courante (en édition), disponible
            if (excludeRouteId.HasValue && delivery.RouteId == excludeRouteId.Value)
                return true;

            // Vérifier le statut de la route
            if (delivery.Route == null)
                return true;

            // ✅ Disponible si route Draft ou Cancelled
            // ❌ Verrouillée si route Confirmed, InProgress, ou Completed
            return delivery.Route.Status == RouteStatus.Draft ||
                   delivery.Route.Status == RouteStatus.Cancelled;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking delivery availability for route: DeliveryId={DeliveryId}", deliveryId);
            return false;
        }
    }
    
    public async Task<RouteDto?> GetActiveRouteForDeliveryAsync(int deliveryId)
    {
        try
        {
            var delivery = await context.Deliveries
                .Include(d => d.Route)
                .ThenInclude(r => r!.Vehicle)
                .FirstOrDefaultAsync(d => d.Id == deliveryId);

            if (delivery?.Route == null)
                return null;

            // Si route Draft ou Cancelled, pas verrouillée
            if (delivery.Route.Status == RouteStatus.Draft ||
                delivery.Route.Status == RouteStatus.Cancelled)
                return null;

            // Route active qui verrouille la livraison
            return new RouteDto
            {
                Id = delivery.Route.Id,
                Reference = delivery.Route.Reference,
                Date = delivery.Route.Date,
                VehicleId = delivery.Route.VehicleId,
                VehicleName = $"{delivery.Route.Vehicle.Brand} {delivery.Route.Vehicle.Model}",
                Status = delivery.Route.Status,
                StatusDisplay = delivery.Route.Status.Humanize(),
                StartTime = delivery.Route.StartTime,
                TotalDeliveries = delivery.Route.TotalDeliveries
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting active route for delivery: DeliveryId={DeliveryId}", deliveryId);
            return null;
        }
    }
    
    public async Task<ResponseResult<List<DeliveryDto>>> GetAvailableDeliveriesForRouteAsync(
        DateTime date,
        int? currentRouteId = null)
    {
        try
        {
            var query = context.Deliveries
                .Include(d => d.Client)
                .Include(d => d.ClientAddress)
                .Include(d => d.Store)
                .Include(d => d.Route)
                .Include(d => d.TimeSlot)
                .Include(d => d.Items)
                .Where(d =>
                    d.Type == DeliveryType.Standard 
                    && d.ScheduledDate.HasValue &&
                    d.ScheduledDate.Value.Date == date.Date &&
                    d.Status == DeliveryStatus.Confirmed)
                .AsQueryable();

            // Filtrer les livraisons disponibles
            if (currentRouteId.HasValue)
            {
                // En mode édition : inclure les livraisons de cette route
                query = query.Where(d =>
                        d.RouteId == null || // Pas de route
                        d.RouteId == currentRouteId || // Route courante
                        d.Route!.Status == RouteStatus.Draft || // Route Draft
                        d.Route!.Status == RouteStatus.Cancelled // Route Cancelled
                );
            }
            else
            {
                // En mode création : exclure les livraisons dans routes actives
                query = query.Where(d =>
                        d.RouteId == null || // Pas de route
                        d.Route!.Status == RouteStatus.Draft || // Route Draft
                        d.Route!.Status == RouteStatus.Cancelled // Route Cancelled
                );
            }

            var deliveries = await query
                .OrderBy(d => d.TimeSlotId)
                .ThenBy(d => d.ScheduledDate)
                .ToListAsync();

            var dtos = deliveries.Select(d => new DeliveryDto
            {
                Id = d.Id,
                Reference = d.Reference,
                SequentialNumber = d.SequentialNumber,

                // Client
                ClientId = d.ClientId,
                ClientName = d.Client.DisplayName,
                ClientPhone = d.Client.Phone,
                ClientEmail = d.Client.Email,

                // Address
                ClientAddressId = d.ClientAddressId,
                Address = d.ClientAddress.Address,
                ZipCode = d.ClientAddress.ZipCode,
                City = d.ClientAddress.City,
                AddressComplement = d.ClientAddress.Complement ?? string.Empty,
                AddressLabel = d.ClientAddress.Label ?? "Principal",

                // Store
                StoreId = d.StoreId,
                StoreName = d.Store.Name,

                // Details
                FileNumber = d.FileNumber,
                ScheduledDate = d.ScheduledDate,
                Price = d.Price,
                Status = d.Status,
                StatusDisplay = d.Status.Humanize(),
                Type = d.Type,
                TypeDisplay = d.Type.Humanize(),

                // Route info (pour affichage si déjà assignée)
                RouteId = d.RouteId,
                RouteReference = d.Route?.Reference,

                // Timing
                EstimatedDurationMinutes = d.EstimatedDurationMinutes,
                Latitude = d.ClientAddress.Latitude,
                Longitude = d.ClientAddress.Longitude,
                TimeSlotId = d.TimeSlotId,
                TimeSlot = d.TimeSlot == null
                    ? null
                    : new TimeSlotDto
                    {
                        Id = d.TimeSlot.Id,
                        Name = d.TimeSlot.Name,
                        StartTime = d.TimeSlot.StartTime,
                        EndTime = d.TimeSlot.EndTime
                    },

                WithAssembly = d.WithAssembly,
                DeliveryNotes = d.DeliveryNotes ?? string.Empty,
                InternalNotes = d.InternalNotes ?? string.Empty,

                // Items
                Items = d.Items.Select(i => new DeliveryItemDto
                {
                    Id = i.Id,
                    Reference = i.Reference ?? string.Empty,
                    Designation = i.Designation,
                    Quantity = i.Quantity,
                    Information = i.Information ?? string.Empty
                }).ToList(),
                TotalPackages = d.TotalPackages,

                CreatedDate = d.CreatedDate,
                CreatedBy = d.CreatedBy
            }).ToList();

            logger.LogInformation(
                "Found {Count} available deliveries for date {Date} (CurrentRouteId={RouteId})",
                dtos.Count, date.ToString("yyyy-MM-dd"), currentRouteId);

            return ResponseResult<List<DeliveryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting available deliveries for route");
            return ResponseResult<List<DeliveryDto>>.Failure(
                "Erreur lors de la récupération des livraisons disponibles");
        }
    }

    public async Task<ResponseResult<DeliveryDto>> GeocodeDeliveryAsync(int id)
    {
        try
        {
            var delivery = await context.Deliveries
                .Include(d => d.ClientAddress)
                .Include(d => d.Client)
                .Include(d => d.Store)
                .Include(d => d.Items)
                .Include(d => d.Route)
                .Include(d => d.TimeSlot)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (delivery == null)
                return ResponseResult<DeliveryDto>.Failure("Livraison introuvable");

            var geocode = await geocodingService.GeocodeAddressAsync(
                delivery.ClientAddress.Address,
                delivery.ClientAddress.ZipCode,
                delivery.ClientAddress.City);

            if (!geocode.Latitude.HasValue || !geocode.Longitude.HasValue)
                return ResponseResult<DeliveryDto>.Failure(
                    $"Adresse introuvable : {delivery.ClientAddress.Address}, {delivery.ClientAddress.ZipCode} {delivery.ClientAddress.City}");

            delivery.ClientAddress.Latitude = geocode.Latitude;
            delivery.ClientAddress.Longitude = geocode.Longitude;

            await context.SaveChangesAsync();

            logger.LogInformation(
                "Geocoded delivery {Id}: ({Lat}, {Lng})",
                id, geocode.Latitude, geocode.Longitude);

            var dto = await GetDeliveryByIdAsync(id);
            return dto.Succeeded && dto.Data != null
                ? ResponseResult<DeliveryDto>.Success(dto.Data)
                : ResponseResult<DeliveryDto>.Failure("Erreur lors du rechargement de la livraison");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error geocoding delivery {Id}", id);
            return ResponseResult<DeliveryDto>.Failure($"Erreur lors du géocodage : {ex.Message}");
        }
    }

    // Private helper methods (unchanged)
    private async Task<int> GetOrCreateClientAsync(
        DeliveryBaseDto dto,
        int tenantId,
        string userId)
    {
        if (dto.ClientId.HasValue)
        {
            var exists = await context.Clients.AnyAsync(c => c.Id == dto.ClientId.Value);
            if (!exists)
                throw new UnauthorizedAccessException("Client introuvable dans le tenant courant");
            return dto.ClientId.Value;
        }

        var client = new Client
        {
            TenantId = tenantId,
            FirstName = dto.ClientFirstName,
            LastName = dto.ClientLastName,
            Phone = dto.ClientPhone,
            Email = dto.ClientEmail,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = userId,
            ModifiedDate = DateTime.UtcNow,
            ModifiedBy = userId,
        };

        context.Clients.Add(client);
        await context.SaveChangesAsync();

        return client.Id;
    }

    private async Task<int> GetOrCreateAddressAsync(
        DeliveryBaseDto dto,
        int clientId)
    {
        if (dto.ClientAddressId.HasValue)
        {
            var exists = await context.ClientAddresses.AnyAsync(a => a.Id == dto.ClientAddressId.Value && a.ClientId == clientId);
            if (!exists)
                throw new UnauthorizedAccessException("Adresse introuvable pour ce client");
            return dto.ClientAddressId.Value;
        }

        var geocodeAddress = await geocodingService.GeocodeAddressAsync(
            dto.Address,
            dto.ZipCode,
            dto.City);

        var address = new ClientAddress
        {
            ClientId = clientId,
            Label = dto.AddressLabel ?? "Principal",
            Address = dto.Address,
            ZipCode = dto.ZipCode,
            City = dto.City,
            Complement = dto.AddressComplement,
            Latitude = geocodeAddress.Latitude,
            Longitude = geocodeAddress.Longitude,
            IsDefault = true
        };

        context.ClientAddresses.Add(address);
        await context.SaveChangesAsync();

        return address.Id;
    }

    private Task UpdateDeliveryItemsAsync(Delivery delivery, List<UpdateDeliveryItemDto> itemsDto)
    {
        var itemIdsToKeep = itemsDto
            .Where(i => i.Id.HasValue)
            .Select(i => i.Id.Value)
            .ToList();

        var itemsToRemove = delivery.Items
            .Where(i => !itemIdsToKeep.Contains(i.Id))
            .ToList();

        foreach (var item in itemsToRemove)
        {
            context.DeliveryItems.Remove(item);
        }

        foreach (var itemDto in itemsDto)
        {
            if (itemDto.Id.HasValue)
            {
                var existingItem = delivery.Items.FirstOrDefault(i => i.Id == itemDto.Id.Value);

                if (existingItem == null) continue;

                existingItem.Reference = itemDto.Reference;
                existingItem.Designation = itemDto.Designation;
                existingItem.Quantity = itemDto.Quantity;
                existingItem.Information = itemDto.Information;
            }
            else
            {
                delivery.Items.Add(new DeliveryItem
                {
                    Reference = itemDto.Reference,
                    Designation = itemDto.Designation,
                    Quantity = itemDto.Quantity,
                    Information = itemDto.Information
                });
            }
        }

        return Task.CompletedTask;
    }

    private static IQueryable<Delivery> ApplySorting(
        IQueryable<Delivery> query,
        DeliveryFilterDto filter)
    {
        var desc = filter.SortDescending;

        return filter.SortBy switch
        {
            nameof(DeliveryViewDto.ClientName) => desc
                ? query.OrderByDescending(d => d.Client.FirstName)
                : query.OrderBy(d => d.Client.FirstName),

            nameof(DeliveryViewDto.City) => desc
                ? query.OrderByDescending(d => d.ClientAddress.City)
                : query.OrderBy(d => d.ClientAddress.City),

            nameof(DeliveryViewDto.Price) => desc
                ? query.OrderByDescending(d => d.Price)
                : query.OrderBy(d => d.Price),

            nameof(DeliveryViewDto.ScheduledDate) => desc
                ? query.OrderByDescending(d => d.ScheduledDate ?? DateTime.MaxValue)
                : query.OrderBy(d => d.ScheduledDate ?? DateTime.MaxValue),

            nameof(DeliveryViewDto.CreatedDate) => desc
                ? query.OrderByDescending(d => d.CreatedDate)
                : query.OrderBy(d => d.CreatedDate),

            nameof(DeliveryViewDto.Status) => desc
                ? query.OrderByDescending(d => d.Status)
                : query.OrderBy(d => d.Status),

            nameof(DeliveryViewDto.SequentialNumber) or _ => desc
                ? query.OrderByDescending(d => d.SequentialNumber)
                : query.OrderBy(d => d.SequentialNumber)
        };
    }
}