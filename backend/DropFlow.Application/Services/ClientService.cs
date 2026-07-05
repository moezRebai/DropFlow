using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Entities;
using DropFlow.Shared.Enums;
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
    // ----------------------------------------------------------------
    // CREATE
    // ----------------------------------------------------------------

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

            // Cr�er le client
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

            // Cr�er la premi�re adresse avec g�ocodage
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

            logger.LogInformation("Client {ClientId} cr�� par {UserId}", client.Id, currentUser.Id);

            return ResponseResult<int>.Success(client.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la cr�ation du client");
            return ResponseResult<int>.Failure("Erreur lors de la cr�ation du client");
        }
    }

    // ----------------------------------------------------------------
    // READ
    // ----------------------------------------------------------------

    public async Task<ResponseResult<ClientDto>> GetClientByIdAsync(int id)
    {
        try
        {
            var client = await context.Clients
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
                return ResponseResult<ClientDto>.Failure("Client non trouv�");

            var dto = await MapToDtoAsync(client);

            return ResponseResult<ClientDto>.Success(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la r�cup�ration du client {Id}", id);
            return ResponseResult<ClientDto>.Failure("Erreur lors de la r�cup�ration");
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

            // Recherche � pas de ToLower() ni StringComparison : SQL Server est d�j� CI par d�faut
            var term = searchTerm.Trim();
            query = query.Where(c =>
                c.FirstName.Contains(term) ||
                c.LastName.Contains(term) ||
                c.Phone.Contains(term) ||
                (c.Email != null && c.Email.Contains(term)));

            var clients = await query
                .Take(10) // Limiter � 10 r�sultats pour auto-compl�tion
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
                    Address = a.Address,
                    ZipCode = a.ZipCode,
                    City = a.City,
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
            logger.LogError(ex, "Erreur lors de la r�cup�ration des adresses du client {ClientId}", clientId);
            return [];
        }
    }

    // ----------------------------------------------------------------
    // UPDATE
    // ----------------------------------------------------------------

    public async Task<ResponseResult> UpdateClientAsync(int id, UpdateClientDto dto)
    {
        try
        {
            var client = await context.Clients.FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
                return ResponseResult.Failure("Client non trouv�");

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

            logger.LogInformation("Client {ClientId} mis � jour par {UserId}", client.Id, currentUser.Id);

            return ResponseResult.Success("Client mis � jour avec succ�s");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la mise � jour du client {Id}", id);
            return ResponseResult.Failure("Erreur lors de la mise � jour");
        }
    }

    // ----------------------------------------------------------------
    // ADDRESS MANAGEMENT
    // ----------------------------------------------------------------

    public async Task<ResponseResult<int>> AddAddressAsync(int clientId, CreateClientAddressDto dto)
    {
        try
        {
            var client = await context.Clients.FirstOrDefaultAsync(c => c.Id == clientId);
            if (client == null)
                return ResponseResult<int>.Failure("Client non trouv�");

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult<int>.Failure("User not found");

            // G�ocodage
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

            logger.LogInformation("Adresse ajout�e au client {ClientId} par {UserId}", clientId, currentUser.Id);

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
                return ResponseResult.Failure("Client non trouv�");

            var address = client.Addresses.FirstOrDefault(a => a.Id == addressId);
            if (address == null)
                return ResponseResult.Failure("Adresse non trouv�e");

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            // R�initialiser toutes les adresses
            foreach (var addr in client.Addresses)
            {
                addr.IsDefault = false;
            }

            // D�finir la nouvelle par d�faut
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

            logger.LogInformation("Adresse par d�faut chang�e pour client {ClientId} par {UserId}", clientId,
                currentUser.Id);

            return ResponseResult.Success("Adresse par d�faut mise � jour");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors du changement d'adresse par d�faut");
            return ResponseResult.Failure("Erreur lors du changement d'adresse par d�faut");
        }
    }

    public async Task<ResponseResult> DeleteAddressAsync(int clientId, int addressId)
    {
        try
        {
            var address = await context.ClientAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.ClientId == clientId);

            if (address == null)
                return ResponseResult.Failure("Adresse non trouv�e");

            // V�rifier si l'adresse est utilis�e par des livraisons
            var hasDeliveries = await context.Deliveries
                .AnyAsync(d => d.ClientAddressId == addressId);

            if (hasDeliveries)
            {
                return ResponseResult.Failure(
                    "Impossible de supprimer cette adresse car elle est utilis�e par des livraisons");
            }

            // V�rifier que ce n'est pas la derni�re adresse
            var addressCount = await context.ClientAddresses
                .CountAsync(a => a.ClientId == clientId);

            if (addressCount <= 1)
            {
                return ResponseResult.Failure(
                    "Impossible de supprimer la derni�re adresse du client");
            }

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            context.ClientAddresses.Remove(address);
            await context.SaveChangesAsync();

            // Si c'�tait l'adresse par d�faut, d�finir une nouvelle par d�faut
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

            logger.LogInformation("Adresse {AddressId} supprim�e du client {ClientId} par {UserId}",
                addressId, clientId, currentUser.Id);

            return ResponseResult.Success("Adresse supprim�e avec succ�s");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la suppression de l'adresse");
            return ResponseResult.Failure("Erreur lors de la suppression");
        }
    }

    // ----------------------------------------------------------------
    // DELETE
    // ----------------------------------------------------------------

    public async Task<ResponseResult> DeleteClientAsync(int id)
    {
        try
        {
            var client = await context.Clients.FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
                return ResponseResult.Failure("Client non trouv�");

            // V�rifier si le client a des livraisons
            var hasDeliveries = await context.Deliveries
                .AnyAsync(d => d.ClientId == id);

            if (hasDeliveries)
            {
                return ResponseResult.Failure(
                    "Impossible de supprimer ce client car il a des livraisons. " +
                    "Vous pouvez le d�sactiver � la place.");
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

            logger.LogInformation("Client {ClientId} supprim� par {UserId}", id, currentUser.Id);

            return ResponseResult.Success("Client supprim� avec succ�s");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la suppression du client {Id}", id);
            return ResponseResult.Failure("Erreur lors de la suppression");
        }
    }

    // ----------------------------------------------------------------
// NOUVELLES M�THODES � AJOUTER DANS ClientService.cs (Backend)
// Placez ces m�thodes dans la section READ (apr�s SearchClientsAsync)
// ----------------------------------------------------------------

    /// <summary>
    /// R�cup�re la liste compl�te des clients avec pagination et filtres
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

            // Filtre par recherche (Nom, Email, T�l�phone)
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
            logger.LogError(ex, "Erreur lors de la r�cup�ration de la liste des clients");
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
    /// R�cup�re l'historique des livraisons d'un client
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
            logger.LogError(ex, "Erreur lors de la r�cup�ration des livraisons du client {ClientId}", clientId);
            return [];
        }
    }

// ----------------------------------------------------------------
// NOUVELLE M�THODE � AJOUTER DANS LA SECTION ADDRESSES (apr�s AddAddressAsync)
// ----------------------------------------------------------------

    /// <summary>
    /// Met � jour une adresse client existante
    /// </summary>
    public async Task<ResponseResult> UpdateAddressAsync(int clientId, int addressId, UpdateClientAddressDto dto)
    {
        try
        {
            var address = await context.ClientAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.ClientId == clientId);

            if (address == null)
                return ResponseResult.Failure("Adresse non trouv�e");

            var currentUser = tenantService.GetCurrentUser();
            if (currentUser == null)
                return ResponseResult.Failure("User not found");

            // G�ocoder la nouvelle adresse si elle a chang�
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

            // Mettre � jour les champs
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

            logger.LogInformation("Adresse {AddressId} mise � jour pour client {ClientId} par {UserId}",
                addressId, clientId, currentUser.Id);

            return ResponseResult.Success("Adresse mise � jour avec succ�s");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la mise � jour de l'adresse");
            return ResponseResult.Failure("Erreur lors de la mise � jour");
        }
    }
    // ----------------------------------------------------------------
    // PRIVATE HELPERS
    // ----------------------------------------------------------------

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