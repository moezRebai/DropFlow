using System.Net.Http.Json;
using DropFlow.Application.Interfaces;
using DropFlow.Domain.Maps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DropFlow.Infrastructure.Services.Geocoding;

public class GeocodingService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<GeocodingService> logger)
    : IGeocodingService
{
    private readonly string? _googleMapsApiKey = configuration["Google:MapsApiKey"];

    /// <summary>
    /// Géocode une adresse en utilisant l'API Google Maps Geocoding
    /// Retourne les coordonnées GPS (latitude, longitude) ou (null, null) en cas d'erreur
    /// </summary>
    public async Task<GeocodeAddress> GeocodeAddressAsync(
        string address, 
        string zipCode, 
        string city)
    {
        try
        {
            // Vérifier que la clé API est configurée
            if (string.IsNullOrEmpty(_googleMapsApiKey))
            {
                logger.LogWarning("Google Maps API Key non configurée dans appsettings.json");
                return new GeocodeAddress(null, null);
            }

            // Construire l'adresse complète
            var fullAddress = $"{address}, {zipCode} {city}, France";
            var encodedAddress = Uri.EscapeDataString(fullAddress);

            // Appel API Google Maps Geocoding
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={_googleMapsApiKey}";
            
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Erreur API Google Maps: {StatusCode}", response.StatusCode);
                return new GeocodeAddress(null, null);
            }

            var result = await response.Content.ReadFromJsonAsync<GoogleMapsGeocodingResponse>();
            
            if (result == null || result.Status != "OK" || result.Results == null || !result.Results.Any())
            {
                logger.LogWarning("Aucun résultat de géocodage pour l'adresse: {Address}, Reason : {Message}", fullAddress, result?.ErrorMessage);
                return new GeocodeAddress(null, null);
            }

            var location = result.Results[0].Geometry?.Location;
            
            if (location == null)
            {
                logger.LogWarning("Location non trouvée dans la réponse Google Maps");
                return new GeocodeAddress(null, null);
            }

            logger.LogInformation("Adresse géocodée avec succès: {Address} -> ({Lat}, {Lng})", 
                fullAddress, location.Lat, location.Lng);

            return new GeocodeAddress(location.Lat, location.Lng);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Erreur HTTP lors du géocodage de l'adresse");
            return new GeocodeAddress(null, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors du géocodage de l'adresse: {Address}, {ZipCode} {City}", 
                address, zipCode, city);
            return new GeocodeAddress(null, null);
        }
    }

    public async Task<(GoogleDirectionsResponse Response, string ErrorMessage)> GetOptimizedRouteAsync(string originAddress, string wayPoints,
        bool optimize = true)
    {
        if (string.IsNullOrWhiteSpace(_googleMapsApiKey))
        {
            return (new GoogleDirectionsResponse(),
                "Google Maps API Key non configurée dans appsettings.json");
        }

        var destination = originAddress;

        var optimizeParam = optimize ? "optimize:true" : "optimize:false";

        var url =
            $"https://maps.googleapis.com/maps/api/directions/json" +
            $"?origin={Uri.EscapeDataString(originAddress)}" +
            $"&waypoints={optimizeParam}|{wayPoints}" + // ← Utiliser la variable
            $"&destination={Uri.EscapeDataString(destination)}" +
            $"&key={_googleMapsApiKey}" +
            $"&language=fr";

        logger.LogInformation("Google Directions API - Optimize={Optimize}, Origin={Origin}", optimize, originAddress);
        
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var response = await httpClient.GetAsync(url, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                return (new GoogleDirectionsResponse(),
                    $"Erreur Google Maps (Code {response.StatusCode}) : {errorContent}");
            }

            var result = await response.Content
                .ReadFromJsonAsync<GoogleDirectionsResponse>(cancellationToken: cts.Token);

            if (result == null)
            {
                return (new GoogleDirectionsResponse(),
                    "Réponse invalide de Google Maps.");
            }

            return (result, string.Empty);
        }
        catch (TaskCanceledException)
        {
            return (new GoogleDirectionsResponse(),
                "L'optimisation a pris trop de temps. Réessayez avec moins de livraisons.");
        }
        catch (HttpRequestException)
        {
            return (new GoogleDirectionsResponse(),
                "Erreur de connexion à Google Maps. Vérifiez votre connexion Internet.");
        }
        catch (Exception ex)
        {
            return (new GoogleDirectionsResponse(),
                $"Erreur HTTP lors du géocodage de l'adresse ({ex.Message})");
        }
    }
    
    public async Task<(GoogleDirectionsResponse Response, string ErrorMessage)> GetDirectionsAsync(
        string originAddress, 
        string destinationAddress)
    {
        if (string.IsNullOrWhiteSpace(_googleMapsApiKey))
        {
            return (new GoogleDirectionsResponse(),
                "Google Maps API Key non configurée dans appsettings.json");
        }

        var url =
            $"https://maps.googleapis.com/maps/api/directions/json" +
            $"?origin={Uri.EscapeDataString(originAddress)}" +
            $"&destination={Uri.EscapeDataString(destinationAddress)}" +
            $"&key={_googleMapsApiKey}" +
            $"&language=fr";

        logger.LogInformation("Calling Google Directions API (simple route): {Origin} -> {Destination}", 
            originAddress, destinationAddress);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var response = await httpClient.GetAsync(url, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cts.Token);
                return (new GoogleDirectionsResponse(),
                    $"Erreur Google Maps (Code {response.StatusCode}) : {errorContent}");
            }

            var result = await response.Content
                .ReadFromJsonAsync<GoogleDirectionsResponse>(cancellationToken: cts.Token);

            if (result == null)
            {
                return (new GoogleDirectionsResponse(),
                    "Réponse invalide de Google Maps.");
            }

            return (result, string.Empty);
        }
        catch (TaskCanceledException)
        {
            return (new GoogleDirectionsResponse(),
                "Le calcul d'itinéraire a pris trop de temps. Réessayez.");
        }
        catch (HttpRequestException)
        {
            return (new GoogleDirectionsResponse(),
                "Erreur de connexion à Google Maps. Vérifiez votre connexion Internet.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors du calcul d'itinéraire");
            return (new GoogleDirectionsResponse(),
                $"Erreur lors du calcul d'itinéraire : {ex.Message}");
        }
    }

}