using DropFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropFlow.Infrastructure.Data.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        // Table
        builder.ToTable("Stores");

        // Primary Key
        builder.HasKey(s => s.Id);

        // ════════════════════════════════════════════════════════════════
        // PROPERTIES - REQUIRED
        // ════════════════════════════════════════════════════════════════

        builder.Property(s => s.TenantId)
            .IsRequired();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.Property(s => s.CreatedDate)
            .IsRequired();

        // ════════════════════════════════════════════════════════════════
        // PROPERTIES - NULLABLE (Optionnels)
        // ════════════════════════════════════════════════════════════════

        builder.Property(s => s.ContactName)
            .IsRequired(false)  // ✅ NULLABLE
            .HasMaxLength(200);

        builder.Property(s => s.Phone)
            .IsRequired(false)  // ✅ NULLABLE
            .HasMaxLength(20);

        builder.Property(s => s.Email)
            .IsRequired(false)  // ✅ NULLABLE
            .HasMaxLength(100);

        builder.Property(s => s.Address)
            .IsRequired(false)  // ✅ NULLABLE
            .HasMaxLength(500);

        builder.Property(s => s.ZipCode)
            .IsRequired(false)  // ✅ NULLABLE
            .HasMaxLength(10);

        builder.Property(s => s.City)
            .IsRequired(false)  // ✅ NULLABLE
            .HasMaxLength(100);

        builder.Property(s => s.Latitude)
            .IsRequired(false)  // ✅ NULLABLE
            .HasColumnType("decimal(10,7)");

        builder.Property(s => s.Longitude)
            .IsRequired(false)  // ✅ NULLABLE
            .HasColumnType("decimal(10,7)");

        builder.Property(s => s.Notes)
            .IsRequired(false)  // ✅ NULLABLE
            .HasMaxLength(2000);

        builder.Property(s => s.CreatedBy)
            .IsRequired(false)
            .HasMaxLength(450);

        builder.Property(s => s.ModifiedDate)
            .IsRequired(false);

        builder.Property(s => s.ModifiedBy)
            .IsRequired(false)
            .HasMaxLength(450);

        // ════════════════════════════════════════════════════════════════
        // RELATIONSHIPS
        // ════════════════════════════════════════════════════════════════

        // Deliveries (One-to-Many)
        builder.HasMany(s => s.Deliveries)
            .WithOne(d => d.Store)
            .HasForeignKey(d => d.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        // ════════════════════════════════════════════════════════════════
        // INDEXES
        // ════════════════════════════════════════════════════════════════

        builder.HasIndex(s => s.TenantId)
            .HasDatabaseName("IX_Stores_TenantId");

        builder.HasIndex(s => new { s.Name, s.TenantId })
            .HasDatabaseName("IX_Stores_Name_TenantId");

        builder.HasIndex(s => s.Phone)
            .HasDatabaseName("IX_Stores_Phone");

        builder.HasIndex(s => s.Email)
            .HasDatabaseName("IX_Stores_Email");
    }
}