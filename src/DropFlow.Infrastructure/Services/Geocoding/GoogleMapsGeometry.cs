using System.Text.Json.Serialization;

namespace DropFlow.Infrastructure.Services.Geocoding;

internal class GoogleMapsGeometry
{
    [JsonPropertyName("location")]
    public GoogleMapsLocation? Location { get; set; }

    [JsonPropertyName("location_type")]
    public string LocationType { get; set; } = string.Empty;
}