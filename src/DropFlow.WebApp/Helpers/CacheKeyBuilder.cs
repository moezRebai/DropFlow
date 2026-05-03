namespace DropFlow.WebApp.Helpers;

/// <summary>
/// Construction cohérente des clés de cache
/// </summary>
public static class CacheKeyBuilder
{
    /// <summary>
    /// Clé pour liste complète : "clients_all", "stores_all"
    /// </summary>
    public static string All(string entityName) => $"{entityName.ToLower()}_all";

    /// <summary>
    /// Clé pour entité individuelle : "client_123", "store_456"
    /// </summary>
    public static string ById(string entityName, int id) => $"{entityName.ToLower()}_{id}";

    /// <summary>
    /// Clé pour recherche/lookup : "client_search_dupont"
    /// </summary>
    public static string Search(string entityName, string searchTerm) => 
        $"{entityName.ToLower()}_search_{searchTerm.Trim().ToLowerInvariant()}";

    /// <summary>
    /// Clé pour relation : "client_123_deliveries"
    /// </summary>
    public static string Related(string entityName, int id, string relation) => 
        $"{entityName.ToLower()}_{id}_{relation.ToLower()}";

    /// <summary>
    /// Préfixe pour invalidation en masse : "client_*"
    /// </summary>
    public static string Prefix(string entityName) => $"{entityName.ToLower()}_";
}