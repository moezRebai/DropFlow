using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Entities;
using DropFlow.Domain.Enums;
using DropFlow.Shared.Clients;
using DropFlow.Shared.Common;
using DropFlow.Shared.Deliveries;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Application.Services;

public class ClientService(
    IApplicationDbContext context,
    ITenantService tenantService,
    IAuditService auditService,
    IGeocodingService geocodingService,
    IValidator<CreateClientDto> createValidator,
    ILogger<ClientService> logger)
    : IClientService
{
    // ════════════════════════════════════════════════════════════════
    // CREATE
    // ════════════════════════════════════════════════════════════════

    public async Task<ResponseResult<int>> CreateClientAsync(CreateClientDto dto)
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

            // Créer le client
            var client = new Client
            {
                TenantId = tenantId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Email = dto.Email,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = currentUser.Id
            };

            context.Clients.Add(client);
            await context.SaveChangesAsync();

            // Créer la première adresse avec géocodage
            var geocode = await geocodingService.GeocodeAddressAsync(
                dto.Address.Address,
                dto.Address.ZipCode,
                dto.Address.City);

            var address = new ClientAddress
            {
                ClientId = client.Id,
                Label = dto.Address.Label ?? "Principal",
                Address = dto.Address.Address,
                ZipCode = dto.Address.ZipCode,
                City = dto.Address.City,
                Complement = dto.Address.Complement,
                Latitude = geocode.Latitude,
                Longitude = geocode.Longitude,
                IsDefault = true
            };

            context.ClientAddresses.Add(address);
            await context.SaveChangesAsync();

            // Audit
            await auditService.LogAsync(
                tenantId: tenantId,
                userId: currentUser.Id,
                action: "ClientCreated",
                entityName: nameof(Client),
                entityId: client.Id,
                changes: new
                {
                    Name = client.DisplayName,
                    client.Phone
                },
                severity: AuditSeverity.Info
            );

            logger.LogInformation("Client {ClientId} créé par {UserId}", client.Id, currentUser.Id);

            return ResponseResult<int>.Success(client.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la création du client");
            return ResponseResult<int>.Failure("Erreur lors de la création du client");
        }
    }

    // ════════════════════════════════════════════════════════════════
    // READ
    // ════════════════════════════════════════════════════════════════

    public async Task<ResponseResult<ClientDto>> GetClientByIdAsync(int id)
    {
        try
        {
            var client = await context.Clients
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
                return ResponseResult<ClientDto>.Failure("Client non trouvé");

            var dto = await MapToDtoAsync(client);

            return ResponseResult<ClientDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération du client {Id}", id);
            return ResponseResult<ClientDto>.Failure("Erreur lors de la récupération");
        }
    }

    public async Task<List<ClientLookupDto>> SearchClientsAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<ClientLookupDto>();

            var query = context.Clients
                .Include(c => c.Addresses)
                .Where(c => c.IsActive)
                .AsQueryable();

            // Recherche
            searchTerm = searchTerm.ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(searchTerm) ||
                c.LastName.ToLower().Contains(searchTerm) ||
                c.Phone.Contains(searchTerm) ||
                (c.Email != null && c.Email.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)));

            var clients = await query
                .Take(10) // Limiter à 10 résultats pour auto-complétion
                .ToListAsync();

            var results = clients.Select(c => new ClientLookupDto
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                Phone = c.Phone,
                Email = c.Email,
                Addresses = c.Addresses.Select(a => new ClientAddressLookupDto
                {
                    Id = a.Id,
                    Label = a.Label,
                    FullAddress = $"{a.Address}, {a.ZipCode} {a.City}",
                    IsDefault = a.IsDefault
                }).ToList()
            }).ToList();

            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la recherche de clients");
            return [];
        }
    }

    public async Task<List<ClientAddressDto>> GetClientAddressesAsync(int clientId)
    {
        try
        {
            var addresses = await context.ClientAddresses
                .Where(a => a.ClientId == clientId)
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.Label)
                .Select(a => new ClientAddressDto
                {
                    Id = a.Id,
                    ClientId = a.ClientId,
                    Label = a.Label,
                    Address = a.Address,
                    ZipCode = a.ZipCode,
                    City = a.City,
                    Complement = a.Complement,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    IsDefault = a.IsDefault
                })
                .ToListAsync();

            return addresses;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des adresses du client {ClientId}", clientId);
            return [];
        }
    }

    // ════════════════════════════════════════════════════════════════
    // UPDATE
    // ════════════════════════════════════════════════════════════════

    public async Task<ResponseResult> UpdateClientAsync(int id, UpdateClientDto dto)
    {
        try
        {
            var client = await context.Clients.FindAsync(id);

            if (client == null)
                return ResponseResult.Failure("Client non trouvé");

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            var oldValues = new
            {
                client.FirstName,
                client.LastName,
                client.Phone,
                client.Email
            };

            // Update properties
            client.FirstName = dto.FirstName;
            client.LastName = dto.LastName;
            client.Phone = dto.Phone;
            client.Email = dto.Email;
            client.IsActive = dto.IsActive;
            client.ModifiedDate = DateTime.UtcNow;
            client.ModifiedBy = currentUser.Id;

            await context.SaveChangesAsync();

            // Audit
            await auditService.LogAsync(
                tenantId: client.TenantId,
                userId: currentUser.Id,
                action: "ClientUpdated",
                entityName: nameof(Client),
                entityId: client.Id,
                changes: new
                {
                    Old = oldValues,
                    New = new { dto.FirstName, dto.LastName, dto.Phone, dto.Email }
                },
                severity: AuditSeverity.Info
            );

            logger.LogInformation("Client {ClientId} mis à jour par {UserId}", client.Id, currentUser.Id);

            return ResponseResult.Success("Client mis à jour avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la mise à jour du client {Id}", id);
            return ResponseResult.Failure("Erreur lors de la mise à jour");
        }
    }

    // ════════════════════════════════════════════════════════════════
    // ADDRESS MANAGEMENT
    // ════════════════════════════════════════════════════════════════

    public async Task<ResponseResult<int>> AddAddressAsync(int clientId, CreateClientAddressDto dto)
    {
        try
        {
            var client = await context.Clients.FindAsync(clientId);
            if (client == null)
                return ResponseResult<int>.Failure("Client non trouvé");

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult<int>.Failure("User not found");

            // Géocodage
            var geocode = await geocodingService.GeocodeAddressAsync(
                dto.Address,
                dto.ZipCode,
                dto.City);

            var address = new ClientAddress
            {
                ClientId = clientId,
                Label = dto.Label ?? "Nouvelle adresse",
                Address = dto.Address,
                ZipCode = dto.ZipCode,
                City = dto.City,
                Complement = dto.Complement,
                Latitude = geocode.Latitude,
                Longitude = geocode.Longitude,
                IsDefault = false
            };

            context.ClientAddresses.Add(address);
            await context.SaveChangesAsync();

            // Audit
            await auditService.LogAsync(
                tenantId: client.TenantId,
                userId: currentUser.Id,
                action: "ClientAddressAdded",
                entityName: nameof(ClientAddress),
                entityId: address.Id,
                changes: new { ClientId = clientId, address.Label, address.City },
                severity: AuditSeverity.Info
            );

            logger.LogInformation("Adresse ajoutée au client {ClientId} par {UserId}", clientId, currentUser.Id);

            return ResponseResult<int>.Success(address.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'ajout de l'adresse");
            return ResponseResult<int>.Failure("Erreur lors de l'ajout de l'adresse");
        }
    }

    public async Task<ResponseResult> SetDefaultAddressAsync(int clientId, int addressId)
    {
        try
        {
            var client = await context.Clients
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == clientId);

            if (client == null)
                return ResponseResult.Failure("Client non trouvé");

            var address = client.Addresses.FirstOrDefault(a => a.Id == addressId);
            if (address == null)
                return ResponseResult.Failure("Adresse non trouvée");

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            // Réinitialiser toutes les adresses
            foreach (var addr in client.Addresses)
            {
                addr.IsDefault = false;
            }

            // Définir la nouvelle par défaut
            address.IsDefault = true;

            await context.SaveChangesAsync();

            // Audit
            await auditService.LogAsync(
                tenantId: client.TenantId,
                userId: currentUser.Id,
                action: "ClientDefaultAddressChanged",
                entityName: nameof(ClientAddress),
                entityId: addressId,
                changes: new { ClientId = clientId, AddressLabel = address.Label },
                severity: AuditSeverity.Info
            );

            logger.LogInformation("Adresse par défaut changée pour client {ClientId} par {UserId}", clientId,
                currentUser.Id);

            return ResponseResult.Success("Adresse par défaut mise à jour");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors du changement d'adresse par défaut");
            return ResponseResult.Failure("Erreur lors du changement d'adresse par défaut");
        }
    }

    public async Task<ResponseResult> DeleteAddressAsync(int clientId, int addressId)
    {
        try
        {
            var address = await context.ClientAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.ClientId == clientId);

            if (address == null)
                return ResponseResult.Failure("Adresse non trouvée");

            // Vérifier si l'adresse est utilisée par des livraisons
            var hasDeliveries = await context.Deliveries
                .AnyAsync(d => d.ClientAddressId == addressId);

            if (hasDeliveries)
            {
                return ResponseResult.Failure(
                    "Impossible de supprimer cette adresse car elle est utilisée par des livraisons");
            }

            // Vérifier que ce n'est pas la dernière adresse
            var addressCount = await context.ClientAddresses
                .CountAsync(a => a.ClientId == clientId);

            if (addressCount <= 1)
            {
                return ResponseResult.Failure(
                    "Impossible de supprimer la dernière adresse du client");
            }

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            context.ClientAddresses.Remove(address);
            await context.SaveChangesAsync();

            // Si c'était l'adresse par défaut, définir une nouvelle par défaut
            if (address.IsDefault)
            {
                var newDefault = await context.ClientAddresses
                    .FirstOrDefaultAsync(a => a.ClientId == clientId);

                if (newDefault != null)
                {
                    newDefault.IsDefault = true;
                    await context.SaveChangesAsync();
                }
            }

            // Audit
            await auditService.LogAsync(
                tenantId: tenantService.GetTenantId(),
                userId: currentUser.Id,
                action: "ClientAddressDeleted",
                entityName: nameof(ClientAddress),
                entityId: addressId,
                changes: new { ClientId = clientId, AddressLabel = address.Label },
                severity: AuditSeverity.Warning
            );

            logger.LogInformation("Adresse {AddressId} supprimée du client {ClientId} par {UserId}",
                addressId, clientId, currentUser.Id);

            return ResponseResult.Success("Adresse supprimée avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la suppression de l'adresse");
            return ResponseResult.Failure("Erreur lors de la suppression");
        }
    }

    // ════════════════════════════════════════════════════════════════
    // DELETE
    // ════════════════════════════════════════════════════════════════

    public async Task<ResponseResult> DeleteClientAsync(int id)
    {
        try
        {
            var client = await context.Clients.FindAsync(id);

            if (client == null)
                return ResponseResult.Failure("Client non trouvé");

            // Vérifier si le client a des livraisons
            var hasDeliveries = await context.Deliveries
                .AnyAsync(d => d.ClientId == id);

            if (hasDeliveries)
            {
                return ResponseResult.Failure(
                    "Impossible de supprimer ce client car il a des livraisons. " +
                    "Vous pouvez le désactiver à la place.");
            }

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            var clientName = client.DisplayName;

            context.Clients.Remove(client);
            await context.SaveChangesAsync();

            // Audit
            await auditService.LogAsync(
                tenantId: client.TenantId,
                userId: currentUser.Id,
                action: "ClientDeleted",
                entityName: nameof(Client),
                entityId: id,
                changes: new { ClientName = clientName },
                severity: AuditSeverity.Warning
            );

            logger.LogInformation("Client {ClientId} supprimé par {UserId}", id, currentUser.Id);

            return ResponseResult.Success("Client supprimé avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la suppression du client {Id}", id);
            return ResponseResult.Failure("Erreur lors de la suppression");
        }
    }

    // ════════════════════════════════════════════════════════════════
// NOUVELLES MÉTHODES À AJOUTER DANS ClientService.cs (Backend)
// Placez ces méthodes dans la section READ (après SearchClientsAsync)
// ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Récupère la liste complète des clients avec pagination et filtres
    /// </summary>
    public async Task<PagedResult<ClientDto>> GetClientsAsync(ClientFilterDto filter)
    {
        try
        {
            var tenantId = tenantService.GetTenantId();

            var query = context.Clients
                .Include(c => c.Addresses)
                .Where(c => c.TenantId == tenantId)
                .AsQueryable();

            // Filtre par recherche (Nom, Email, Téléphone)
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(c =>
                    c.FirstName.ToLower().Contains(searchTerm) ||
                    c.LastName.ToLower().Contains(searchTerm) ||
                    c.Phone.Contains(searchTerm) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchTerm)));
            }

            // Compter le total AVANT pagination
            var totalCount = await query.CountAsync();

            // Pagination
            var clients = await query
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            // Mapper vers DTO avec statistiques
            var clientDtos = new List<ClientDto>();
            foreach (var client in clients)
            {
                var dto = await MapToDtoAsync(client);
                clientDtos.Add(dto);
            }

            return new PagedResult<ClientDto>
            {
                Items = clientDtos,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération de la liste des clients");
            return new PagedResult<ClientDto>
            {
                Items = [],
                TotalCount = 0,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
    }

    /// <summary>
    /// Récupère l'historique des livraisons d'un client
    /// </summary>
    public async Task<List<DeliveryDto>> GetClientDeliveriesAsync(int clientId)
    {
        try
        {
            var deliveries = await context.Deliveries
                .Include(d => d.Store)
                .Where(d => d.ClientId == clientId)
                .OrderByDescending(d => d.ScheduledDate)
                .Select(d => new DeliveryDto
                {
                    Id = d.Id,
                    Reference = d.Reference,
                    ClientId = d.ClientId,
                    StoreId = d.StoreId,
                    StoreName = d.Store != null ? d.Store.Name : null,
                    ScheduledDate = d.ScheduledDate,
                    CreatedDate = d.CreatedDate,
                    Status = d.Status,
                    Price = d.Price,
                    DeliveryNotes = d.DeliveryNotes,
                    InternalNotes = d.InternalNotes,
                    // Ajouter d'autres champs selon votre DeliveryDto
                })
                .ToListAsync();

            return deliveries;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des livraisons du client {ClientId}", clientId);
            return [];
        }
    }

// ════════════════════════════════════════════════════════════════
// NOUVELLE MÉTHODE À AJOUTER DANS LA SECTION ADDRESSES (après AddAddressAsync)
// ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Met à jour une adresse client existante
    /// </summary>
    public async Task<ResponseResult> UpdateAddressAsync(int clientId, int addressId, UpdateClientAddressDto dto)
    {
        try
        {
            var address = await context.ClientAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.ClientId == clientId);

            if (address == null)
                return ResponseResult.Failure("Adresse non trouvée");

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            // Géocoder la nouvelle adresse si elle a changé
            if (address.Address != dto.Address ||
                address.ZipCode != dto.ZipCode ||
                address.City != dto.City)
            {
                var geocode = await geocodingService.GeocodeAddressAsync(
                    dto.Address,
                    dto.ZipCode,
                    dto.City);

                address.Latitude = geocode.Latitude;
                address.Longitude = geocode.Longitude;
            }

            // Mettre à jour les champs
            address.Label = dto.Label;
            address.Address = dto.Address;
            address.ZipCode = dto.ZipCode;
            address.City = dto.City;
            address.Complement = dto.Complement;

            await context.SaveChangesAsync();

            // Audit
            await auditService.LogAsync(
                tenantId: tenantService.GetTenantId(),
                userId: currentUser.Id,
                action: "ClientAddressUpdated",
                entityName: nameof(ClientAddress),
                entityId: addressId,
                changes: new
                {
                    ClientId = clientId,
                    AddressLabel = address.Label,
                    NewAddress = $"{dto.Address}, {dto.ZipCode} {dto.City}"
                },
                severity: AuditSeverity.Info
            );

            logger.LogInformation("Adresse {AddressId} mise à jour pour client {ClientId} par {UserId}",
                addressId, clientId, currentUser.Id);

            return ResponseResult.Success("Adresse mise à jour avec succès");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la mise à jour de l'adresse");
            return ResponseResult.Failure("Erreur lors de la mise à jour");
        }
    }
    // ════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════════════════════

    private async Task<ClientDto> MapToDtoAsync(Client client)
    {
        // Calculer les statistiques
        var deliveryStats = await context.Deliveries
            .Where(d => d.ClientId == client.Id)
            .GroupBy(d => 1)
            .Select(g => new
            {
                TotalDeliveries = g.Count(),
                TotalRevenue = g.Sum(d => d.Price)
            })
            .FirstOrDefaultAsync();

        return new ClientDto
        {
            Id = client.Id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Phone = client.Phone,
            Email = client.Email,
            IsActive = client.IsActive,
            Addresses = client.Addresses.Select(a => new ClientAddressDto
            {
                Id = a.Id,
                ClientId = a.ClientId,
                Label = a.Label,
                Address = a.Address,
                ZipCode = a.ZipCode,
                City = a.City,
                Complement = a.Complement,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                IsDefault = a.IsDefault
            }).ToList(),
            TotalDeliveries = deliveryStats?.TotalDeliveries ?? 0,
            TotalRevenue = deliveryStats?.TotalRevenue ?? 0,
            CreatedDate = client.CreatedDate
        };
    }
}