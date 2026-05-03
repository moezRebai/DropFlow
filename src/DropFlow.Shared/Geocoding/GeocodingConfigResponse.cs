namespace DropFlow.Shared.Geocoding;

public class GeocodingConfigResponse
{
    public bool IsConfigured { get; set; }
    public bool ApiKeyPresent { get; set; }
    public string? ApiKeyMasked { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ConfigPath { get; set; } = string.Empty;
    public string? Instructions { get; set; }
}