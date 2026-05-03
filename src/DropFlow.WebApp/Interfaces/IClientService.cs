using DropFlow.Shared.Clients;
using DropFlow.Shared.Common;
using DropFlow.Shared.Deliveries;

namespace DropFlow.WebApp.Interfaces;

/// <summary>
/// Interface du service de gestion des clients côté frontend
/// </summary>
public interface IClientService
{
    #region READ Operations

    /// <summary>
    /// Récupère la liste complète des clients avec pagination et filtres
    /// </summary>
    Task<PagedResult<ClientDto>> GetClientsAsync(ClientFilterDto filter);

    /// <summary>
    /// Récupère un client par son ID avec ses statistiques
    /// </summary>
    Task<ClientDto?> GetClientByIdAsync(int id);

    /// <summary>
    /// Recherche des clients (autocomplete)
    /// </summary>
    Task<List<ClientLookupDto>> SearchClientsAsync(string searchTerm);

    /// <summary>
    /// Récupère les adresses d'un client
    /// </summary>
    Task<List<ClientAddressDto>> GetClientAddressesAsync(int clientId);

    /// <summary>
    /// Récupère l'historique des livraisons d'un client
    /// </summary>
    Task<List<DeliveryDto>> GetClientDeliveriesAsync(int clientId);

    #endregion

    #region UPDATE Operations

    /// <summary>
    /// Met à jour les informations d'un client
    /// </summary>
    Task<ResponseResult> UpdateClientAsync(int id, UpdateClientDto dto);

    /// <summary>
    /// Ajoute une nouvelle adresse à un client
    /// </summary>
    Task<ResponseResult<int>> AddAddressAsync(int clientId, CreateClientAddressDto dto);

    /// <summary>
    /// Met à jour une adresse existante
    /// </summary>
    Task<ResponseResult> UpdateAddressAsync(int clientId, int addressId, UpdateClientAddressDto dto);

    /// <summary>
    /// Définit une adresse comme adresse par défaut
    /// </summary>
    Task<ResponseResult> SetDefaultAddressAsync(int clientId, int addressId);

    /// <summary>
    /// Supprime une adresse
    /// </summary>
    Task<ResponseResult> DeleteAddressAsync(int clientId, int addressId);

    #endregion

    #region DELETE Operations

    /// <summary>
    /// Supprime un client (vérifie les livraisons)
    /// </summary>
    Task<ResponseResult> DeleteClientAsync(int id);

    #endregion

    #region Cache Management

    /// <summary>
    /// Invalide tout le cache clients
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// Invalide le cache d'un client spécifique
    /// </summary>
    void InvalidateClientCache(int id);

    /// <summary>
    /// Force le rechargement depuis l'API
    /// </summary>
    Task RefreshAsync();

    #endregion
}