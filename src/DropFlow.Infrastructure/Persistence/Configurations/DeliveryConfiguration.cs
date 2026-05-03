using DropFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropFlow.Infrastructure.Persistence.Configurations;

public class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("Deliveries");
        builder.HasKey(d => d.Id);
        
        // ═══ INDEXES ═══
        
        builder.HasIndex(d => d.TenantId);
        builder.HasIndex(d => d.Reference).IsUnique();
        builder.HasIndex(d => d.ClientId);
        builder.HasIndex(d => d.ClientAddressId);
        builder.HasIndex(d => d.StoreId); 
        builder.HasIndex(d => d.ScheduledDate);
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => new { d.TenantId, d.ScheduledDate });
        builder.HasIndex(d => new { d.TenantId, d.Status });
        builder.HasIndex(d => d.TimeSlotId);
        builder.HasIndex(d => d.Type);
        builder.HasIndex(d => d.RouteId);
        builder.HasIndex(d => d.UrgentDriverId);
        builder.HasIndex(d => d.SequenceOrder); // ✅ NOUVEAU - Index pour tri par ordre
        
        // ═══ PROPERTIES ═══
        
        builder.Property(d => d.Reference)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.FileNumber)
            .HasMaxLength(50);
        
        builder.Property(d => d.Price)
            .HasPrecision(18, 2)
            .IsRequired();
        
        builder.Property(d => d.ClientPaymentAmount)
            .HasPrecision(18, 2);
        
        builder.Property(d => d.StorePaymentAmount)
            .HasPrecision(18, 2);
        
        builder.Property(d => d.Status)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(d => d.DeliveryNotes)
            .HasMaxLength(2000);
        
        builder.Property(d => d.InternalNotes)
            .HasMaxLength(2000);
        
        builder.Property(d => d.SignatureUrl)
            .HasMaxLength(500);
        
        builder.Property(d => d.PhotoUrl)
            .HasMaxLength(500);
        
        builder.Property(d => d.CreatedBy)
            .HasMaxLength(450)
            .IsRequired();
        
        builder.Property(d => d.ModifiedBy)
            .HasMaxLength(450);
        
        builder.Property(d => d.EstimatedDurationMinutes)
            .IsRequired(false); // Nullable mais sera validé avec ScheduledDate

        builder.Property(d => d.TimeSlotId)
            .IsRequired(false);
        
        // ═══ ✨ NOUVEAUX CHAMPS - OPTIMISATION ═══
        
        builder.Property(d => d.DepartureAddress)
            .HasMaxLength(500);
        
        builder.Property(d => d.DepartureTime);
        
        builder.Property(d => d.TravelDurationMinutes);
        
        builder.Property(d => d.DistanceToNextMeters);
        
        // ═══ RELATIONS ═══
        
        // Store (Required)
        builder.HasOne(d => d.Store)
            .WithMany(s => s.Deliveries)
            .HasForeignKey(d => d.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // FK vers RouteSheet
        builder.HasOne(d => d.Route)
            .WithMany(rs => rs.Deliveries)
            .HasForeignKey(d => d.RouteId)
            .OnDelete(DeleteBehavior.SetNull);

        // FK vers UrgentDriver
        builder.HasOne(d => d.UrgentDriver)
            .WithMany(dr => dr.UrgentDeliveries)
            .HasForeignKey(d => d.UrgentDriverId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Items (Cascade)
        builder.HasMany(d => d.Items)
            .WithOne(i => i.Delivery)
            .HasForeignKey(i => i.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(d => d.TimeSlot)
            .WithMany(t => t.Deliveries)
            .HasForeignKey(d => d.TimeSlotId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.Property(d => d.ValidationComment)
            .HasMaxLength(1000);
        
        builder.Property(d => d.IsClientAbsent)
            .IsRequired()
            .HasDefaultValue(false);
        
        builder.HasIndex(d => d.ValidatedByDriverId);
        
        // FK vers Driver (validateur)
        // ClientSetNull: EF handles the null in C# before saving, avoiding SQL Server's
        // restriction against multiple SET NULL cascade paths from Deliveries → Drivers.
        builder.HasOne(d => d.ValidatedByDriver)
            .WithMany()
            .HasForeignKey(d => d.ValidatedByDriverId)
            .OnDelete(DeleteBehavior.ClientSetNull);
        
        // ═══ IGNORE CALCULATED ═══
        
        builder.Ignore(d => d.TotalPackages);
    }
}