namespace DropFlow.Shared.Geocoding;

public class GeocodeResponseDto
{
    public bool Success { get; set; }
    public string Address { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public string Message { get; set; } = string.Empty;
}