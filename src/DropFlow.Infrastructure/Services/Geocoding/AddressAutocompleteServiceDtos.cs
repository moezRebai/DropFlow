using System.Text.Json.Serialization;

namespace DropFlow.Infrastructure.Services.Geocoding;

// ════════════════════════════════════════════════════════════════
// GOOGLE PLACES API RESPONSE MODELS
// ════════════════════════════════════════════════════════════════

internal class PlacesAutocompleteResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("predictions")]
    public List<Prediction>? Predictions { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

internal class Prediction
{
    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("structured_formatting")]
    public StructuredFormatting? StructuredFormatting { get; set; }
}

internal class StructuredFormatting
{
    [JsonPropertyName("main_text")]
    public string MainText { get; set; } = string.Empty;

    [JsonPropertyName("secondary_text")]
    public string SecondaryText { get; set; } = string.Empty;
}

internal class PlaceDetailsResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public PlaceResult? Result { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

internal class PlaceResult
{
    [JsonPropertyName("formatted_address")]
    public string FormattedAddress { get; set; } = string.Empty;

    [JsonPropertyName("address_components")]
    public List<AddressComponent>? AddressComponents { get; set; }

    [JsonPropertyName("geometry")]
    public PlaceGeometry? Geometry { get; set; }
}

internal class AddressComponent
{
    [JsonPropertyName("long_name")]
    public string LongName { get; set; } = string.Empty;

    [JsonPropertyName("short_name")]
    public string ShortName { get; set; } = string.Empty;

    [JsonPropertyName("types")]
    public List<string> Types { get; set; } = new();
}

internal class PlaceGeometry
{
    [JsonPropertyName("location")]
    public PlaceLocation? Location { get; set; }
}

internal class PlaceLocation
{
    [JsonPropertyName("lat")]
    public decimal Lat { get; set; }

    [JsonPropertyName("lng")]
    public decimal Lng { get; set; }
}