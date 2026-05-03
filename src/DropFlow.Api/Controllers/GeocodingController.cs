using DropFlow.Application.Interfaces;
using DropFlow.Shared.Geocoding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class GeocodingController(
    IGeocodingService geocodingService,
    IAddressAutocompleteService autocompleteService,
    ILogger<GeocodingController> logger) : ControllerBase
{
    // ════════════════════════════════════════════════════════════════
    // GEOCODING TEST
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test du service de géocodage avec une adresse complète
    /// </summary>
    /// <remarks>
    /// Exemple de requête:
    /// GET /api/test/geocode?address=123 Rue de Rivoli&amp;zipCode=75001&amp;city=Paris
    /// </remarks>
    [HttpGet("geocode")]
    [ProducesResponseType(typeof(GeocodeResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestGeocode(
        [FromQuery] string address,
        [FromQuery] string zipCode,
        [FromQuery] string city)
    {
        logger.LogInformation("Test géocodage: {Address}, {ZipCode} {City}", address, zipCode, city);

        var result = await geocodingService.GeocodeAddressAsync(address, zipCode, city);

        var response = new GeocodeResponseDto
        {
            Success = result is { Latitude: not null, Longitude: not null },
            Address = address,
            ZipCode = zipCode,
            City = city,
            FullAddress = $"{address}, {zipCode} {city}",
            Latitude = result.Latitude,
            Longitude = result.Longitude,
            GoogleMapsUrl = result is { Latitude: not null, Longitude: not null }
                ? $"https://www.google.com/maps?q={result.Latitude},{result.Longitude}"
                : null,
            Message = result.Latitude.HasValue
                ? "Géocodage réussi"
                : "Géocodage échoué - Vérifiez la clé API Google Maps dans appsettings.json"
        };

        return Ok(response);
    }

    /// <summary>
    /// Test du service de géocodage avec des adresses prédéfinies
    /// </summary>
    [HttpGet("geocode/examples")]
    [ProducesResponseType(typeof(List<GeocodeResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestGeocodeExamples()
    {
        var testAddresses = new List<(string Address, string ZipCode, string City)>
        {
            ("1 Avenue des Champs-Élysées", "75008", "Paris"),
            ("Tour Eiffel", "75007", "Paris"),
            ("10 Rue de la République", "69001", "Lyon"),
            ("La Canebière", "13001", "Marseille"),
            ("Place Stanislas", "54000", "Nancy")
        };

        var results = new List<GeocodeResponseDto>();

        foreach (var (address, zipCode, city) in testAddresses)
        {
            logger.LogInformation("Test géocodage exemple: {Address}, {ZipCode} {City}", 
                address, zipCode, city);

            var result = await geocodingService.GeocodeAddressAsync(address, zipCode, city);

            results.Add(new GeocodeResponseDto
            {
                Success = result.Latitude.HasValue && result.Longitude.HasValue,
                Address = address,
                ZipCode = zipCode,
                City = city,
                FullAddress = $"{address}, {zipCode} {city}",
                Latitude = result.Latitude,
                Longitude = result.Longitude,
                GoogleMapsUrl = result.Latitude.HasValue && result.Longitude.HasValue
                    ? $"https://www.google.com/maps?q={result.Latitude},{result.Longitude}"
                    : null,
                Message = result.Latitude.HasValue
                    ? "Géocodage réussi"
                    : "Géocodage échoué"
            });

            // Délai pour éviter de saturer l'API Google
            await Task.Delay(200);
        }

        var summary = new
        {
            TotalTests = results.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            SuccessRate = $"{(results.Count(r => r.Success) * 100.0 / results.Count):F1}%",
            Results = results
        };

        return Ok(summary);
    }

    /// <summary>
    /// Vérifier la configuration de la clé API Google Maps
    /// </summary>
    [HttpGet("geocode/config")]
    [ProducesResponseType(typeof(GeocodingConfigResponse), StatusCodes.Status200OK)]
    public IActionResult CheckGeocodingConfig([FromServices] IConfiguration configuration)
    {
        var apiKey = configuration["Google:MapsApiKey"];
        var isConfigured = !string.IsNullOrEmpty(apiKey);

        var response = new GeocodingConfigResponse
        {
            IsConfigured = isConfigured,
            ApiKeyPresent = isConfigured,
            ApiKeyMasked = isConfigured ? MaskApiKey(apiKey!) : null,
            Message = isConfigured
                ? "✅ Clé API Google Maps configurée"
                : "❌ Clé API Google Maps non configurée dans appsettings.json",
            ConfigPath = "Google:MapsApiKey",
            Instructions = !isConfigured
                ? "Ajoutez la clé API dans appsettings.json : { \"Google\": { \"MapsApiKey\": \"VOTRE_CLE\" } }"
                : null
        };

        return Ok(response);
    }

   // ════════════════════════════════════════════════════════════════
    // PLACES AUTOCOMPLETE TEST (NOUVEAU)
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Test de l'autocomplete d'adresse avec Places API
    /// </summary>
    /// <remarks>
    /// Exemple: GET /api/test/autocomplete?input=123 rue de riv
    /// </remarks>
    [HttpGet("autocomplete")]
    [ProducesResponseType(typeof(AutocompleteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestAutocomplete([FromQuery] string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return BadRequest(new { Error = "Le paramètre 'input' est requis" });
        }

        logger.LogInformation("Test autocomplete: {Input}", input);

        var suggestions = await autocompleteService.GetSuggestionsAsync(input);

        var response = new AutocompleteResponse
        {
            Success = suggestions.Any(),
            Input = input,
            SuggestionsCount = suggestions.Count,
            Suggestions = suggestions,
            Message = suggestions.Any()
                ? $"Trouvé {suggestions.Count} suggestion(s)"
                : "Aucune suggestion trouvée - Vérifiez Places API"
        };

        return Ok(response);
    }

    /// <summary>
    /// Test de récupération des détails d'une adresse via Place ID
    /// </summary>
    /// <remarks>
    /// Exemple: GET /api/test/place-details?placeId=ChIJD7fiBh9u5kcRYJSMaMOCCwQ
    /// </remarks>
    [HttpGet("place-details")]
    [ProducesResponseType(typeof(PlaceDetailsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestPlaceDetails([FromQuery] string placeId)
    {
        if (string.IsNullOrWhiteSpace(placeId))
        {
            return BadRequest(new { Error = "Le paramètre 'placeId' est requis" });
        }

        logger.LogInformation("Test place details: {PlaceId}", placeId);

        var details = await autocompleteService.GetPlaceDetailsAsync(placeId);

        var response = new PlaceDetailsResponse
        {
            Success = details != null,
            PlaceId = placeId,
            Details = details,
            Message = details != null
                ? "Détails récupérés avec succès"
                : "Impossible de récupérer les détails - Vérifiez le Place ID"
        };

        return Ok(response);
    }

    /// <summary>
    /// Test complet : Autocomplete + Détails
    /// Simule le workflow utilisateur complet
    /// </summary>
    [HttpGet("autocomplete/full-test")]
    [ProducesResponseType(typeof(FullAutocompleteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestFullAutocomplete([FromQuery] string input = "Tour Eiffel")
    {
        logger.LogInformation("Test complet autocomplete: {Input}", input);

        var response = new FullAutocompleteResponse
        {
            Step1_Input = input
        };

        // Step 1: Autocomplete
        var suggestions = await autocompleteService.GetSuggestionsAsync(input);
        response.Step2_SuggestionsCount = suggestions.Count;
        response.Step2_Suggestions = suggestions;

        if (!suggestions.Any())
        {
            response.Success = false;
            response.Message = "❌ Aucune suggestion trouvée";
            return Ok(response);
        }

        // Step 2: Get details du premier résultat
        var firstSuggestion = suggestions.First();
        response.Step3_SelectedSuggestion = firstSuggestion;

        var details = await autocompleteService.GetPlaceDetailsAsync(firstSuggestion.PlaceId);
        response.Step4_AddressDetails = details;

        response.Success = details != null;
        response.Message = details != null
            ? $"✅ Workflow complet réussi : {details.Address}, {details.ZipCode} {details.City}"
            : "❌ Échec de récupération des détails";

        return Ok(response);
    }

    /// <summary>
    /// Test avec plusieurs requêtes d'autocomplete courantes
    /// </summary>
    [HttpGet("autocomplete/examples")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestAutocompleteExamples()
    {
        var testQueries = new List<string>
        {
            "123 rue de riv",
            "Tour Eiffel",
            "Champs Elysees",
            "Gare de Lyon",
            "Avenue des"
        };

        var results = new List<object>();

        foreach (var query in testQueries)
        {
            var suggestions = await autocompleteService.GetSuggestionsAsync(query);
            
            results.Add(new
            {
                Query = query,
                Success = suggestions.Any(),
                suggestions.Count,
                FirstSuggestion = suggestions.FirstOrDefault()?.Description
            });

            await Task.Delay(200); // Rate limiting
        }

        return Ok(new
        {
            TotalTests = results.Count,
            SuccessCount = results.Count(r => (bool)((dynamic)r).Success),
            Results = results
        });
    }
    private static string MaskApiKey(string apiKey)
    {
        if (apiKey.Length <= 8)
            return "***";

        return $"{apiKey[..4]}...{apiKey[^4..]}";
    }
}

// ════════════════════════════════════════════════════════════════
// RESPONSE MODELS
// ════════════════════════════════════════════════════════════════