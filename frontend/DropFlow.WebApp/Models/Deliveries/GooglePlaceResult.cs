namespace DropFlow.WebApp.Models.Deliveries;

public class GooglePlaceResult
{
    public string? FormattedAddress { get; set; }
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}