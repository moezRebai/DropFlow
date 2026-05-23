namespace DropFlow.Shared.Geocoding;

public class PlaceDetailsResponse
{
    public bool Success { get; set; }
    public string PlaceId { get; set; } = string.Empty;
    public AddressDetails? Details { get; set; }
    public string Message { get; set; } = string.Empty;
}