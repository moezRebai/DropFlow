using DropFlow.Application.Interfaces.Routes;
using DropFlow.Application.Interfaces.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropFlow.Infrastructure.Services;

public class RouteReferenceService(
    IApplicationDbContext context,
    ILogger<RouteReferenceService> logger)
    : IRouteReferenceService
{
    /// <summary>
    /// Génère une référence unique pour une tournée au format FR-YYYYMMDD-NNN
    /// Exemple: FR-20250105-001
    /// Le compteur est séquentiel par date et par tenant
    /// </summary>
    public async Task<string> GenerateReferenceAsync(int tenantId, DateTime date)
    {
        try
        {
            var dateString = date.ToString("yyyyMMdd");
            
            // Compter les tournées existantes pour ce tenant à cette date
            var count = await context.Routes
                .Where(r => r.TenantId == tenantId && r.Date.Date == date.Date)
                .CountAsync();
            
            // Incrémenter pour obtenir le prochain numéro
            var sequentialNumber = count + 1;
            
            // Formater avec padding de 3 chiffres (001, 002, etc.)
            var reference = $"RT-{dateString}-{sequentialNumber:D3}";
            
            // Vérifier l'unicité (au cas où)
            var exists = await context.Routes
                .AnyAsync(r => r.TenantId == tenantId && r.Reference == reference);
            
            if (exists)
            {
                // Si existe (cas rare de concurrence), incrémenter jusqu'à trouver un libre
                logger.LogWarning("Référence {Reference} existe déjà, recherche suivante disponible...", reference);
                
                for (var i = sequentialNumber + 1; i < 999; i++)
                {
                    reference = $"FR-{dateString}-{i:D3}";
                    exists = await context.Routes
                        .AnyAsync(r => r.TenantId == tenantId && r.Reference == reference);
                    
                    if (!exists)
                        break;
                }
            }
            
            return reference;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la génération de la référence de tournée");
            throw;
        }
    }
}