using DropFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropFlow.Infrastructure.Persistence.Configurations;

public class ClientAddressConfiguration : IEntityTypeConfiguration<ClientAddress>
{
    public void Configure(EntityTypeBuilder<ClientAddress> builder)
    {
        builder.ToTable("ClientAddresses");
        builder.HasKey(a => a.Id);
        
        builder.HasIndex(a => a.ClientId);
        
        builder.Property(a => a.Label)
            .HasMaxLength(50);
        
        builder.Property(a => a.Address)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.Property(a => a.ZipCode)
            .HasMaxLength(10)
            .IsRequired();
        
        builder.Property(a => a.City)
            .HasMaxLength(100)
            .IsRequired();
        
        builder.Property(a => a.Complement)
            .HasMaxLength(200);
        
        builder.Property(a => a.Latitude)
            .HasPrecision(10, 8);
        
        builder.Property(a => a.Longitude)
            .HasPrecision(11, 8);
        
        // Relations
        builder.HasMany(a => a.Deliveries)
            .WithOne(d => d.ClientAddress)
            .HasForeignKey(d => d.ClientAddressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}