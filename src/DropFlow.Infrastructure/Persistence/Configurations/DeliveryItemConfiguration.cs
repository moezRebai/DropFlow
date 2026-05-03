using DropFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropFlow.Infrastructure.Persistence.Configurations;

public class DeliveryItemConfiguration : IEntityTypeConfiguration<DeliveryItem>
{
    public void Configure(EntityTypeBuilder<DeliveryItem> builder)
    {
        builder.ToTable("DeliveryItems");
        builder.HasKey(i => i.Id);
        
        builder.HasIndex(i => i.DeliveryId);
        
        builder.Property(i => i.Reference)
            .HasMaxLength(100);
        
        builder.Property(i => i.Designation)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(i => i.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();
        
        builder.Property(i => i.Information)
            .HasMaxLength(200);
    }
}