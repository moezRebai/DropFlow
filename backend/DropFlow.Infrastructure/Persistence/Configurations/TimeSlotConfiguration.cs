namespace DropFlow.Infrastructure.Persistence.Configurations;

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
{
    public void Configure(EntityTypeBuilder<TimeSlot> builder)
    {
        builder.ToTable("TimeSlots");
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(t => t.StartTime)
            .IsRequired();
        
        builder.Property(t => t.EndTime)
            .IsRequired();
        
        builder.Property(t => t.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);
        
        // Index pour performance
        builder.HasIndex(t => new { t.TenantId, t.Id });
        
        // Global query filter (multi-tenant)
        builder.HasQueryFilter(t => t.TenantId == 0 || t.TenantId != 0); // Sera remplacé par ITenantService
    }
}