using DropFlow.Shared.Common;

namespace DropFlow.WebApp.Interfaces.Caches;

/// <summary>
/// Interface générique pour services avec cache
/// </summary>
public interface ICachedApiService<TDto, in TFilterDto> 
    where TDto : class
    where TFilterDto : class
{
    /// <summary>
    /// Liste complète (avec cache)
    /// </summary>
    Task<List<TDto>> GetAllAsync();

    /// <summary>
    /// Liste paginée avec filtres (avec cache)
    /// </summary>
    Task<PagedResult<TDto>> GetPagedAsync(TFilterDto filter);

    /// <summary>
    /// Récupère un élément par ID (avec cache)
    /// </summary>
    Task<TDto?> GetByIdAsync(int id);

    /// <summary>
    /// Invalide tout le cache de cette entité
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// Invalide le cache d'un élément spécifique
    /// </summary>
    void InvalidateCacheById(int id);

    /// <summary>
    /// Force un rechargement complet (invalide + recharge)
    /// </summary>
    Task<List<TDto>> RefreshAsync();
}