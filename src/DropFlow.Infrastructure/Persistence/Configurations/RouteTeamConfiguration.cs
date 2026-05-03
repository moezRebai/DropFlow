using DropFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DropFlow.Infrastructure.Persistence.Configurations;

public class RouteTeamConfiguration : IEntityTypeConfiguration<RouteTeam>
{
    public void Configure(EntityTypeBuilder<RouteTeam> builder)
    {
        builder.ToTable("RouteTeam");
        
        builder.HasKey(rsc => rsc.Id);
        
        // FK vers RouteSheet
        builder.HasOne(rsc => rsc.Route)
            .WithMany(rs => rs.Team)
            .HasForeignKey(rsc => rsc.RouteId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // FK vers Driver
        builder.HasOne(rsc => rsc.Driver)
            .WithMany(d => d.RouteAssignments)
            .HasForeignKey(rsc => rsc.DriverId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(rs => rs.TenantId);
        builder.HasIndex(rsc => rsc.RouteId);
        builder.HasIndex(rsc => rsc.DriverId);
        builder.HasIndex(rsc => new { RouteSheetId = rsc.RouteId, rsc.DriverId }).IsUnique();
    }
}