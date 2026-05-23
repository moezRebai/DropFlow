using System.Text.Json.Serialization;

namespace DropFlow.Domain.Maps;

/// <summary>
/// Réponse complète de l'API Google Directions
/// </summary>
public class GoogleDirectionsResponse
{
    [JsonPropertyName("routes")]
    public List<Route>? Routes { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Route calculée par Google Maps
/// </summary>
public class Route
{
    /// <summary>
    /// Ordre optimisé des waypoints (CRITIQUE POUR OPTIMISATION)
    /// </summary>
    [JsonPropertyName("waypoint_order")]
    public int[]? WaypointOrder { get; set; }

    /// <summary>
    /// Segments de la route (legs)
    /// </summary>
    [JsonPropertyName("legs")]
    public List<Leg>? Legs { get; set; }

    /// <summary>
    /// Polyline encodée pour affichage carte
    /// </summary>
    [JsonPropertyName("overview_polyline")]
    public OverviewPolyline? OverviewPolyline { get; set; }

    /// <summary>
    /// Résumé de la route (nom de la route principale)
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// Bounds de la route (pour centrer la carte)
    /// </summary>
    [JsonPropertyName("bounds")]
    public Bounds? Bounds { get; set; }
}

/// <summary>
/// Segment entre deux points (departure -> destination)
/// </summary>
public class Leg
{
    /// <summary>
    /// Distance du segment
    /// </summary>
    [JsonPropertyName("distance")]
    public Distance? Distance { get; set; }

    /// <summary>
    /// Durée du segment
    /// </summary>
    [JsonPropertyName("duration")]
    public Duration? Duration { get; set; }

    /// <summary>
    /// Adresse de départ
    /// </summary>
    [JsonPropertyName("start_address")]
    public string? StartAddress { get; set; }

    /// <summary>
    /// Coordonnées de départ
    /// </summary>
    [JsonPropertyName("start_location")]
    public Location? StartLocation { get; set; }

    /// <summary>
    /// Adresse d'arrivée
    /// </summary>
    [JsonPropertyName("end_address")]
    public string? EndAddress { get; set; }

    /// <summary>
    /// Coordonnées d'arrivée
    /// </summary>
    [JsonPropertyName("end_location")]
    public Location? EndLocation { get; set; }

    /// <summary>
    /// Étapes détaillées (instructions turn-by-turn)
    /// </summary>
    [JsonPropertyName("steps")]
    public List<Step>? Steps { get; set; }
}

/// <summary>
/// Distance d'un segment
/// </summary>
public class Distance
{
    /// <summary>
    /// Texte formaté (ex: "4,9 km")
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Valeur en mètres (ex: 4853)
    /// </summary>
    [JsonPropertyName("value")]
    public int Value { get; set; }
}

/// <summary>
/// Durée d'un segment
/// </summary>
public class Duration
{
    /// <summary>
    /// Texte formaté (ex: "18 minutes")
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Valeur en secondes (ex: 1100)
    /// </summary>
    [JsonPropertyName("value")]
    public int Value { get; set; }
}

/// <summary>
/// Coordonnées GPS (latitude/longitude)
/// </summary>
public class Location
{
    [JsonPropertyName("lat")]
    public decimal Lat { get; set; }

    [JsonPropertyName("lng")]
    public decimal Lng { get; set; }
}

/// <summary>
/// Étape détaillée (turn-by-turn)
/// </summary>
public class Step
{
    [JsonPropertyName("distance")]
    public Distance? Distance { get; set; }

    [JsonPropertyName("duration")]
    public Duration? Duration { get; set; }

    [JsonPropertyName("start_location")]
    public Location? StartLocation { get; set; }

    [JsonPropertyName("end_location")]
    public Location? EndLocation { get; set; }

    [JsonPropertyName("html_instructions")]
    public string? HtmlInstructions { get; set; }

    [JsonPropertyName("travel_mode")]
    public string? TravelMode { get; set; }

    [JsonPropertyName("maneuver")]
    public string? Maneuver { get; set; }
}

/// <summary>
/// Polyline encodée pour affichage sur carte
/// </summary>
public class OverviewPolyline
{
    [JsonPropertyName("points")]
    public string? Points { get; set; }
}

/// <summary>
/// Bounds (limites géographiques) de la route
/// </summary>
public class Bounds
{
    [JsonPropertyName("northeast")]
    public Location? Northeast { get; set; }

    [JsonPropertyName("southwest")]
    public Location? Southwest { get; set; }
}