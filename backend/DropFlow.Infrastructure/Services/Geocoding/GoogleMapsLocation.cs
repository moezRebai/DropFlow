using System.Text.Json.Serialization;

namespace DropFlow.Infrastructure.Services.Geocoding;
internal class GoogleMapsLocation
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}