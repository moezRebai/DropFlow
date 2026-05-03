using DropFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropFlow.Infrastructure.Persistence.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("Drivers");
        
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.UserId)
            .IsRequired()
            .HasMaxLength(450);
        
        builder.Property(d => d.LicenseNumber)
            .HasMaxLength(50);
        
        // FK vers ApplicationUser
        builder.HasOne(d => d.User)
            .WithOne(u => u.Driver)
            .HasForeignKey<Driver>(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(d => d.TenantId);
        builder.HasIndex(d => d.UserId).IsUnique();
        builder.HasIndex(d => d.IsActive);
    }
}