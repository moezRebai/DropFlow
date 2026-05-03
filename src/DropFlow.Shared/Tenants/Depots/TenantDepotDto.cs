

namespace DropFlow.Shared.Tenants.Depots;

/// <summary>
/// DTO pour afficher un dépôt
/// </summary>
public class TenantDepotDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? ZipCode { get; set; }
    
    // Coordonnées GPS
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // Status
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    
    // Audit
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    
    /// <summary>
    /// Adresse complète formatée
    /// </summary>
    public string FormattedAddress
    {
        get
        {
            var parts = new List<string> { FullAddress };
            
            if (!string.IsNullOrEmpty(ZipCode) && !string.IsNullOrEmpty(City))
                parts.Add($"{ZipCode} {City}");
            else if (!string.IsNullOrEmpty(City))
                parts.Add(City);
            
            return string.Join(", ", parts);
        }
    }
    
    /// <summary>
    /// Vérifie si le dépôt a des coordonnées GPS
    /// </summary>
    public bool HasGpsCoordinates => Latitude.HasValue && Longitude.HasValue;
    
    /// <summary>
    /// URL Google Maps pour ce dépôt
    /// </summary>
    public string? GoogleMapsUrl => !HasGpsCoordinates ? null : $"https://www.google.com/maps?q={Latitude},{Longitude}";
}