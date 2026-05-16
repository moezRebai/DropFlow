using System.Reflection;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Common;
using DropFlow.Domain.Constants;
using DropFlow.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DropFlow.Infrastructure.Persistence;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IHttpContextAccessor httpContextAccessor)
    : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
{
    // ═══ EXISTING DBSETS ═══
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    // ═══ PHASE 3 DBSETS ═══
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientAddress> ClientAddresses => Set<ClientAddress>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryItem> DeliveryItems => Set<DeliveryItem>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<RouteTeam> RouteTeams => Set<RouteTeam>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    
    // ═══ ✨ COMPANY SETTINGS ═══
    public DbSet<TenantDepot> TenantDepots => Set<TenantDepot>();

    // ═══ AUTH ═══
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    
        // ═══ GLOBAL QUERY FILTERS ═══
    
        // Existing
        modelBuilder.Entity<UserInvitation>().HasQueryFilter(e =>
            GetCurrentTenantId() == TenantIds.DropFlowAdmin || 
            e.TenantId == GetCurrentTenantId()
        );
    
        modelBuilder.Entity<AuditLog>().HasQueryFilter(e =>
            GetCurrentTenantId() == TenantIds.DropFlowAdmin || 
            e.TenantId == GetCurrentTenantId()
        );

        // Phase 3
        modelBuilder.Entity<Client>().HasQueryFilter(c =>
            GetCurrentTenantId() == TenantIds.DropFlowAdmin || 
            c.TenantId == GetCurrentTenantId()
        );

        modelBuilder.Entity<ClientAddress>().HasQueryFilter(ca =>
            GetCurrentTenantId() == TenantIds.DropFlowAdmin || 
            ca.Client.TenantId == GetCurrentTenantId()
        );

        modelBuilder.Entity<Delivery>().HasQueryFilter(d =>
            GetCurrentTenantId() == TenantIds.DropFlowAdmin || 
            d.TenantId == GetCurrentTenantId()
        );

        modelBuilder.Entity<DeliveryItem>().HasQueryFilter(di =>
            GetCurrentTenantId() == TenantIds.DropFlowAdmin || 
            di.Delivery.TenantId == GetCurrentTenantId()
        );

        modelBuilder.Entity<Store>().HasQueryFilter(s =>
            GetCurrentTenantId() == TenantIds.DropFlowAdmin || 
            s.TenantId == GetCurrentTenantId()
        );

        modelBuilder.Entity<Vehicle>().HasQueryFilter(v =>
            GetCurrentTenantId() == TenantIds.DropFlowAdmin || 
            v.TenantId == GetCurrentTenantId()
        );

        modelBuilder.Entity<Driver>().HasQueryFilter(d =>
            GetCurrentTenantId() == TenantIds.DropFlowAdmin || 
            d.TenantId == GetCurrentTenantId()
        );

        modelBuilder.Entity<RouteTeam>().HasQueryFilter(rsc =>
            GetCurrentTenantId() == TenantIds.DropFlowAdmin || 
            rsc.Driver.TenantId == GetCurrentTenantId()
        );
        
        modelBuilder.Entity<Route>().HasQueryFilter(rs =>
            GetCurrentTenantId() == TenantIds.DropFlowAdmin || 
            rs.TenantId == GetCurrentTenantId()
        );
        
        modelBuilder.Entity<TimeSlot>().HasQueryFilter(r =>
            GetCurrentTenantId() == TenantIds.DropFlowAdmin || 
            r.TenantId == GetCurrentTenantId()
        );
        
        // ═══ ✨ COMPANY SETTINGS - GLOBAL FILTER ═══
        modelBuilder.Entity<TenantDepot>().HasQueryFilter(td =>
            GetCurrentTenantId() == TenantIds.DropFlowAdmin || 
            td.TenantId == GetCurrentTenantId()
        );
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        ApplyTenantId();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantId()
    {
        var tenantId = GetCurrentTenantId();

        if (tenantId == TenantIds.DropFlowAdmin || tenantId == 0)
            return;

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>()
                     .Where(e => e.State == EntityState.Added))
        {
            entry.Entity.TenantId = tenantId;
        }
    }

    private int GetCurrentTenantId()
    {
        var tenantIdClaim = httpContextAccessor.HttpContext?.User
            .FindFirst("TenantId")?.Value;
    
        return int.TryParse(tenantIdClaim, out var tenantId) ? tenantId : 0;
    }
}