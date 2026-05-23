using DropFlow.Application.Interfaces.Deliveries;
using DropFlow.Application.Interfaces.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Infrastructure.Services;

public class DeliveryReferenceService(
    IApplicationDbContext context,
    ILogger<DeliveryReferenceService> logger)
    : IDeliveryReferenceService
{
    private const int MaxAttempts = 10;

    public async Task<string> GenerateReferenceAsync(int tenantId, DateTime? date = null)
    {
        var deliveryDate = date ?? DateTime.UtcNow;
        var dateString = deliveryDate.ToString("yyyyMMdd");

        // Serialize per-tenant-per-day reference generation using PostgreSQL advisory lock.
        // Bit-packs tenantId into upper bits and date integer into lower bits for a unique key.
        var dateInt = deliveryDate.Year * 10000 + deliveryDate.Month * 100 + deliveryDate.Day;
        var lockId = ((long)tenantId << 20) ^ dateInt;

        await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock({0})", lockId);
        try
        {
            var sequentialNumber = await GetNextSequentialNumberAsync(tenantId, deliveryDate);

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var reference = $"DL-{dateString}-{(sequentialNumber + attempt):D3}";

                var exists = await context.Deliveries
                    .AnyAsync(d => d.TenantId == tenantId && d.Reference == reference);

                if (!exists)
                {
                    if (attempt > 0)
                        logger.LogInformation("Référence générée après {Attempts} tentative(s): {Reference}", attempt + 1, reference);

                    return reference;
                }

                logger.LogWarning("Collision (tentative {Attempt}/{Max}): {Reference}", attempt + 1, MaxAttempts, reference);
            }

            throw new InvalidOperationException(
                $"Impossible de générer une référence unique après {MaxAttempts} tentatives");
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock({0})", lockId);
        }
    }

    public async Task<int> GetNextSequentialNumberAsync(int tenantId, DateTime? date = null)
    {
        var deliveryDate = date ?? DateTime.UtcNow;
        var startOfDay = deliveryDate.Date;
        var endOfDay = startOfDay.AddDays(1);

        var maxSequential = await context.Deliveries
            .Where(d => d.TenantId == tenantId
                && d.CreatedDate >= startOfDay
                && d.CreatedDate < endOfDay)
            .MaxAsync(d => (int?)d.SequentialNumber);

        return (maxSequential ?? 0) + 1;
    }
}
