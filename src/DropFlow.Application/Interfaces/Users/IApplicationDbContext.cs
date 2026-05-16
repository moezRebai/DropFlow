using DropFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DropFlow.Application.Interfaces.Users;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantDepot> TenantDepots { get; }
    DbSet<ApplicationUser> Users { get; }
    DbSet<UserInvitation> UserInvitations { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<UserPreferences> UserPreferences { get; } 
    DbSet<Client> Clients { get; }
    DbSet<ClientAddress> ClientAddresses { get; }
    DbSet<Delivery> Deliveries { get; }
    DbSet<DeliveryItem> DeliveryItems { get; }
    DbSet<Store> Stores { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<Driver> Drivers { get; }
    DbSet<Route> Routes { get; }
    DbSet<RouteTeam> RouteTeams { get; }
    DbSet<TimeSlot> TimeSlots { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}