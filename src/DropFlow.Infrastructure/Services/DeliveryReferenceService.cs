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
