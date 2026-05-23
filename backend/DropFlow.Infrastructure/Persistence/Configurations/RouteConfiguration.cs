using DropFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropFlow.Infrastructure.Persistence.Configurations;

public class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("Routes");
        
        builder.HasKey(rs => rs.Id);
        
        builder.Property(rs => rs.Reference)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(rs => rs.TotalDistance)
            .HasPrecision(10, 2);
        
        builder.Property(rs => rs.TotalVolume)
            .HasPrecision(10, 2);
        
        builder.Property(rs => rs.DepartureAddress)
            .HasMaxLength(500);
        
        builder.Property(rs => rs.DepartureLatitude)
            .HasPrecision(10, 8);
        
        builder.Property(rs => rs.DepartureLongitude)
            .HasPrecision(11, 8);
        
        builder.Property(rs => rs.CreatedBy)
            .IsRequired()
            .HasMaxLength(450);
        
        builder.Property(rs => rs.ModifiedBy)
            .HasMaxLength(450);
        
        builder.Property(rs => rs.WasOptimizedByGoogle)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rs => rs.WasManuallyReordered)
            .IsRequired()
            .HasDefaultValue(false);
        
        // FK vers Vehicle
        builder.HasOne(rs => rs.Vehicle)
            .WithMany(v => v.Routes)
            .HasForeignKey(rs => rs.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(rs => rs.TenantId);
        builder.HasIndex(rs => rs.Reference);
        builder.HasIndex(rs => rs.Date);
        builder.HasIndex(rs => rs.VehicleId);
        builder.HasIndex(rs => rs.Status);
    }
}