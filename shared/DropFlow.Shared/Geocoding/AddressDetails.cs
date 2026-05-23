namespace DropFlow.Shared.Geocoding;

/// <summary>
/// Détails complets d'une adresse après sélection
/// </summary>
public class AddressDetails
{
    /// <summary>
    /// Adresse formatée par Google
    /// Ex: "123 Rue de Rivoli, 75001 Paris, France"
    /// </summary>
    public string FormattedAddress { get; set; } = string.Empty;

    /// <summary>
    /// Adresse (numéro + rue)
    /// Ex: "123 Rue de Rivoli"
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Code postal
    /// Ex: "75001"
    /// </summary>
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>
    /// Ville
    /// Ex: "Paris"
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Complément d'adresse (quartier, bâtiment, etc.)
    /// </summary>
    public string? Complement { get; set; }

    /// <summary>
    /// Numéro de rue
    /// Ex: "123"
    /// </summary>
    public string? StreetNumber { get; set; }

    /// <summary>
    /// Nom de la rue
    /// Ex: "Rue de Rivoli"
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// Latitude GPS
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Longitude GPS
    /// </summary>
    public decimal? Longitude { get; set; }
}