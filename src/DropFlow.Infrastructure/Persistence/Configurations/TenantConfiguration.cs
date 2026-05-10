using DropFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropFlow.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        // ═══ EXISTING ═══
        
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.SubDomain)
            .HasMaxLength(50);

        builder.Property(t => t.PlanType)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(t => t.SubDomain)
            .IsUnique()
            .HasFilter("\"SubDomain\" IS NOT NULL");

        builder.HasIndex(t => t.IsActive);
        
        // ═══ NEW - COMPANY INFO ═══
        
        builder.Property(t => t.CompanyName)
            .HasMaxLength(200);
        
        builder.Property(t => t.LogoUrl)
            .HasMaxLength(500);
        
        builder.Property(t => t.Address)
            .HasMaxLength(500);
        
        builder.Property(t => t.ZipCode)
            .HasMaxLength(10);
        
        builder.Property(t => t.City)
            .HasMaxLength(100);
        
        builder.Property(t => t.Phone)
            .HasMaxLength(20);
        
        builder.Property(t => t.Email)
            .HasMaxLength(100);
        
        builder.Property(t => t.Website)
            .HasMaxLength(200);
        
        // ═══ NEW - LEGAL INFO ═══
        
        builder.Property(t => t.Siret)
            .HasMaxLength(14);
        
        builder.Property(t => t.VatNumber)
            .HasMaxLength(13);  // FR + 11 chiffres
        
        builder.Property(t => t.LegalForm)
            .HasMaxLength(50);
        
        builder.Property(t => t.LegalMentions)
            .HasMaxLength(2000);  // Texte plus long
        
        builder.Property(t => t.BankDetails)
            .HasMaxLength(500);
        
        // ═══ INDEXES ═══
        
        builder.HasIndex(t => t.Email);
        builder.HasIndex(t => t.Siret);
    }
}