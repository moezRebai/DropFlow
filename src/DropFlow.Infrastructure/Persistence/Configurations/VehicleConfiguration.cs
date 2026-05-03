using DropFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropFlow.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");
        
        builder.HasKey(v => v.Id);
        
        builder.Property(v => v.Brand)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(v => v.Model)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(v => v.PlateNumber)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.HasIndex(v => v.TenantId);
        builder.HasIndex(v => v.PlateNumber);
        builder.HasIndex(v => v.IsActive);
    }
}