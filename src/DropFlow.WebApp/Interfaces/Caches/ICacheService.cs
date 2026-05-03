namespace DropFlow.WebApp.Interfaces.Caches;

/// <summary>
/// Service de cache générique pour optimiser les performances
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Récupère une valeur du cache
    /// </summary>
    T? Get<T>(string key) where T : class;

    /// <summary>
    /// Ajoute ou met à jour une valeur dans le cache
    /// </summary>
    /// <param name="key">Clé unique du cache</param>
    /// <param name="value">Valeur à cacher</param>
    /// <param name="expiration">Durée d'expiration (défaut: 10 minutes)</param>
    void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Supprime une entrée du cache
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Vérifie si une clé existe dans le cache
    /// </summary>
    bool Exists(string key);

    /// <summary>
    /// Vide tout le cache (pour admin)
    /// </summary>
    void Clear();
}