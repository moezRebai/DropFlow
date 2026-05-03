using System.Text.Json.Serialization;

namespace DropFlow.Infrastructure.Services.Geocoding;

internal class GoogleMapsGeocodingResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("results")]
    public List<GoogleMapsResult>? Results { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}