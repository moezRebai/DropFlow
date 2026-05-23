using DropFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropFlow.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Address)
            .HasMaxLength(500);
        
        builder.Property(u => u.DeletedDate)
            .IsRequired(false);
        
        builder.HasIndex(u => u.DeletedDate);
        builder.HasIndex(u => u.TenantId);
        builder.HasIndex(u => u.Email);
        builder.HasIndex(u => new { u.TenantId, u.Email });
    }
}