using DropFlow.Shared.Common;
using DropFlow.Shared.Stores;

namespace DropFlow.WebApp.Interfaces;

/// <summary>
/// Interface du service de gestion des enseignes avec CRUD complet
/// </summary>
public interface IStoreService
{
    #region READ Operations

    /// <summary>
    /// Récupère toutes les enseignes actives avec cache
    /// </summary>
    /// <param name="forceRefresh">Si true, ignore le cache et recharge depuis l'API</param>
    Task<List<StoreDto>> GetAllStoresAsync(bool forceRefresh = false);

    /// <summary>
    /// Récupère les enseignes avec filtres et pagination
    /// </summary>
    Task<PagedResult<StoreDto>> GetStoresAsync(StoreFilterDto filter);

    /// <summary>
    /// Récupère une enseigne par son ID
    /// </summary>
    Task<StoreDto?> GetStoreByIdAsync(int id);

    /// <summary>
    /// Récupère la liste des enseignes pour lookup (dropdown/autocomplete)
    /// </summary>
    Task<List<StoreLookupDto>> GetStoresLookupAsync();

    #endregion

    #region CREATE Operation

    /// <summary>
    /// Crée une nouvelle enseigne
    /// </summary>
    Task<ResponseResult> CreateStoreAsync(CreateStoreDto dto);

    #endregion

    #region UPDATE Operation

    /// <summary>
    /// Met à jour une enseigne existante
    /// </summary>
    Task<ResponseResult> UpdateStoreAsync(int id, UpdateStoreDto dto);

    #endregion

    #region DELETE Operation

    /// <summary>
    /// Supprime une enseigne (soft ou hard delete selon l'API)
    /// </summary>
    Task<ResponseResult> DeleteStoreAsync(int id);

    #endregion

    #region Cache Management

    /// <summary>
    /// Invalide le cache des enseignes
    /// À appeler après création/modification/suppression
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// Invalide le cache d'une enseigne spécifique
    /// </summary>
    void InvalidateStoreCache(int id);

    /// <summary>
    /// Force le rechargement depuis l'API
    /// </summary>
    Task<List<StoreDto>> RefreshAsync();

    #endregion
}