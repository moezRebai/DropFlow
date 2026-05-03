using DropFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropFlow.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");
        builder.HasKey(c => c.Id);
        
        // Indexes
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => c.Phone);
        builder.HasIndex(c => c.Email);
        builder.HasIndex(c => new { c.TenantId, c.IsActive });
        
        builder.Property(c => c.FirstName)
            .HasMaxLength(100);
        
        builder.Property(c => c.LastName)
            .HasMaxLength(100);
        
        
        builder.Property(c => c.Phone)
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(c => c.Email)
            .HasMaxLength(100);
        
        builder.Property(c => c.CreatedBy)
            .HasMaxLength(450)
            .IsRequired();
        
        builder.Property(c => c.ModifiedBy)
            .HasMaxLength(450);
        
        // Relations
        builder.HasMany(c => c.Addresses)
            .WithOne(a => a.Client)
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(c => c.Deliveries)
            .WithOne(d => d.Client)
            .HasForeignKey(d => d.ClientId)
            .OnDelete(DeleteBehavior.Restrict); // Ne pas supprimer si livraisons
        
        // Ignore calculated
        builder.Ignore(c => c.DisplayName);
    }
}