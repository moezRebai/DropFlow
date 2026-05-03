using System.Text.Json.Serialization;

namespace DropFlow.Infrastructure.Services.Geocoding;

internal class GoogleMapsResult
{
    [JsonPropertyName("formatted_address")]
    public string FormattedAddress { get; set; } = string.Empty;

    [JsonPropertyName("geometry")]
    public GoogleMapsGeometry? Geometry { get; set; }

    [JsonPropertyName("place_id")]
    public string PlaceId { get; set; } = string.Empty;
}