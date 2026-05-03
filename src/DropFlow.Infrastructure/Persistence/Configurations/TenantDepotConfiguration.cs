using DropFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration EF Core pour l'entité TenantDepot
/// </summary>
public class TenantDepotConfiguration : IEntityTypeConfiguration<TenantDepot>
{
    public void Configure(EntityTypeBuilder<TenantDepot> builder)
    {
        builder.ToTable("TenantDepots");
        
        // ═══ PRIMARY KEY ═══
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.Id)
            .ValueGeneratedOnAdd();
        
        // ═══ REQUIRED PROPERTIES ═══
        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(d => d.FullAddress)
            .IsRequired()
            .HasMaxLength(500);
        
        // ═══ OPTIONAL PROPERTIES ═══
        builder.Property(d => d.City)
            .HasMaxLength(100);
        
        builder.Property(d => d.ZipCode)
            .HasMaxLength(10);
        
        // ═══ GPS COORDINATES ═══
        builder.Property(d => d.Latitude)
            .HasPrecision(10, 8);  // Ex: 48.85661400
        
        builder.Property(d => d.Longitude)
            .HasPrecision(11, 8);  // Ex: 2.35222190
        
        // ═══ STATUS FLAGS ═══
        builder.Property(d => d.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);
        
        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        // ═══ AUDIT FIELDS ═══
        builder.Property(d => d.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");
        
        // ═══ RELATIONSHIPS ═══
        builder.HasOne(d => d.Tenant)
            .WithMany(t => t.Depots)
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // ═══ INDEXES ═══
        
        // Index principal sur TenantId (requêtes fréquentes)
        builder.HasIndex(d => d.TenantId)
            .HasDatabaseName("IX_TenantDepots_TenantId");
        
        // Index filtré pour trouver rapidement le dépôt par défaut
        builder.HasIndex(d => new { d.TenantId, d.IsDefault })
            .HasDatabaseName("IX_TenantDepots_IsDefault")
            .HasFilter("[IsDefault] = 1");
        
        // Index pour les dépôts actifs
        builder.HasIndex(d => new { d.TenantId, d.IsActive })
            .HasDatabaseName("IX_TenantDepots_IsActive");
    }
}