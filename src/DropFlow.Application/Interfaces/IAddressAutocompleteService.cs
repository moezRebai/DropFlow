using DropFlow.Shared.Geocoding;

namespace DropFlow.Application.Interfaces;

public interface IAddressAutocompleteService
{
    /// <summary>
    /// Récupère les suggestions d'adresses basées sur l'input utilisateur
    /// </summary>
    /// <param name="input">Texte saisi par l'utilisateur (minimum 3 caractères)</param>
    /// <param name="countryCode">Code pays (par défaut "fr" pour France)</param>
    /// <returns>Liste de suggestions d'adresses</returns>
    Task<List<AddressSuggestion>> GetSuggestionsAsync(string input, string countryCode = "fr");

    /// <summary>
    /// Récupère les détails complets d'une adresse à partir de son Place ID
    /// Inclut : adresse complète, code postal, ville, coordonnées GPS
    /// </summary>
    /// <param name="placeId">ID Google Places de l'adresse</param>
    /// <returns>Détails complets de l'adresse</returns>
    Task<AddressDetails?> GetPlaceDetailsAsync(string placeId);
}