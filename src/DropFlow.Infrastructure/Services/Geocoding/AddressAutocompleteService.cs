using System.Net.Http.Json;
using DropFlow.Application.Interfaces;
using DropFlow.Shared.Geocoding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DropFlow.Infrastructure.Services.Geocoding;

public class AddressAutocompleteService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<AddressAutocompleteService> logger)
    : IAddressAutocompleteService
{
    private readonly string? _apiKey = configuration["Google:MapsApiKey"];

    // ════════════════════════════════════════════════════════════════
    // AUTOCOMPLETE - SUGGESTIONS
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Récupère les suggestions d'adresses via Google Places Autocomplete API
    /// </summary>
    public async Task<List<AddressSuggestion>> GetSuggestionsAsync(
        string input, 
        string countryCode = "fr")
    {
        // Vérifier configuration
        if (string.IsNullOrEmpty(_apiKey))
        {
            logger.LogWarning("Google Maps API Key non configurée - Autocomplete désactivé");
            return [];
        }

        // Validation input
        if (string.IsNullOrWhiteSpace(input) || input.Length < 3)
        {
            return [];
        }

        try
        {
            var encodedInput = Uri.EscapeDataString(input);
            var url = $"https://maps.googleapis.com/maps/api/place/autocomplete/json" +
                      $"?input={encodedInput}" +
                      $"&types=address" +
                      $"&components=country:{countryCode}" +
                      $"&language=fr" +
                      $"&key={_apiKey}";

            logger.LogDebug("Appel Places Autocomplete API pour: {Input}", input);

            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Erreur API Places Autocomplete: {StatusCode}", response.StatusCode);
                return [];
            }

            var result = await response.Content
                .ReadFromJsonAsync<PlacesAutocompleteResponse>();

            if (result == null)
            {
                logger.LogWarning("Réponse null de Places Autocomplete API");
                return [];
            }

            if (result.Status != "OK")
            {
                if (result.Status == "ZERO_RESULTS")
                {
                    logger.LogDebug("Aucun résultat pour: {Input}", input);
                }
                else
                {
                    logger.LogWarning("Places API status: {Status}, Error: {Error}", 
                        result.Status, result.ErrorMessage);
                }
                return [];
            }

            if (result.Predictions == null || !result.Predictions.Any())
            {
                return [];
            }

            var suggestions = result.Predictions.Select(p => new AddressSuggestion
            {
                PlaceId = p.PlaceId,
                Description = p.Description,
                MainText = p.StructuredFormatting?.MainText ?? string.Empty,
                SecondaryText = p.StructuredFormatting?.SecondaryText ?? string.Empty
            }).ToList();

            logger.LogInformation("Trouvé {Count} suggestions pour: {Input}", 
                suggestions.Count, input);

            return suggestions;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Erreur HTTP lors de l'appel à Places Autocomplete API");
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'autocomplete d'adresse pour: {Input}", input);
            return new List<AddressSuggestion>();
        }
    }

    // ════════════════════════════════════════════════════════════════
    // PLACE DETAILS - DÉTAILS COMPLETS
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Récupère les détails complets d'une adresse via Google Places Details API
    /// </summary>
    public async Task<AddressDetails?> GetPlaceDetailsAsync(string placeId)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            logger.LogWarning("Google Maps API Key non configurée");
            return null;
        }

        if (string.IsNullOrWhiteSpace(placeId))
        {
            logger.LogWarning("PlaceId vide fourni à GetPlaceDetailsAsync");
            return null;
        }

        try
        {
            var url = $"https://maps.googleapis.com/maps/api/place/details/json" +
                      $"?place_id={placeId}" +
                      $"&fields=address_components,geometry,formatted_address" +
                      $"&language=fr" +
                      $"&key={_apiKey}";

            logger.LogDebug("Appel Places Details API pour PlaceId: {PlaceId}", placeId);

            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Erreur API Places Details: {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content
                .ReadFromJsonAsync<PlaceDetailsResponse>();

            if (result == null || result.Status != "OK" || result.Result == null)
            {
                logger.LogWarning("Places Details API status: {Status}, Error: {Error}", 
                    result?.Status, result?.ErrorMessage);
                return null;
            }

            var details = ParseAddressComponents(result.Result);

            logger.LogInformation(
                "Détails récupérés pour PlaceId {PlaceId}: {Address}, {ZipCode} {City}", 
                placeId, details.Address, details.ZipCode, details.City);

            return details;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Erreur HTTP lors de l'appel à Places Details API");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de la récupération des détails pour PlaceId: {PlaceId}", placeId);
            return null;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Parse les composants d'adresse Google et extrait les informations utiles
    /// </summary>
    private AddressDetails ParseAddressComponents(PlaceResult place)
    {
        var details = new AddressDetails
        {
            FormattedAddress = place.FormattedAddress,
            Latitude = place.Geometry?.Location?.Lat,
            Longitude = place.Geometry?.Location?.Lng
        };

        if (place.AddressComponents == null || !place.AddressComponents.Any())
        {
            logger.LogWarning("Aucun composant d'adresse trouvé");
            return details;
        }

        foreach (var component in place.AddressComponents)
        {
            // Numéro de rue
            if (component.Types.Contains("street_number"))
            {
                details.StreetNumber = component.LongName;
            }
            
            // Nom de rue
            if (component.Types.Contains("route"))
            {
                details.Route = component.LongName;
            }
            
            // Code postal
            if (component.Types.Contains("postal_code"))
            {
                details.ZipCode = component.LongName;
            }
            
            // Ville
            if (component.Types.Contains("locality"))
            {
                details.City = component.LongName;
            }
            
            // Département (fallback si pas de ville)
            if (component.Types.Contains("administrative_area_level_2") && string.IsNullOrEmpty(details.City))
            {
                details.City = component.LongName;
            }

            // Complément (quartier, etc.)
            if (component.Types.Contains("sublocality") || component.Types.Contains("sublocality_level_1"))
            {
                details.Complement = component.LongName;
            }
        }

        // Construire l'adresse complète
        if (!string.IsNullOrEmpty(details.StreetNumber) && !string.IsNullOrEmpty(details.Route))
        {
            details.Address = $"{details.StreetNumber} {details.Route}".Trim();
        }
        else if (!string.IsNullOrEmpty(details.Route))
        {
            details.Address = details.Route;
        }
        else
        {
            // Fallback sur formatted_address
            details.Address = place.FormattedAddress?.Split(',').FirstOrDefault()?.Trim() ?? string.Empty;
        }

        return details;
    }
}