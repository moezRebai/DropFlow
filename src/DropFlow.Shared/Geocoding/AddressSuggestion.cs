namespace DropFlow.Shared.Geocoding;

/// <summary>
/// Suggestion d'adresse retournée par l'autocomplete
/// </summary>
public class AddressSuggestion
{
    /// <summary>
    /// ID unique Google Places pour récupérer les détails
    /// </summary>
    public string PlaceId { get; set; } = string.Empty;

    /// <summary>
    /// Description complète de l'adresse
    /// Ex: "123 Rue de Rivoli, 75001 Paris, France"
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Partie principale (numéro + rue)
    /// Ex: "123 Rue de Rivoli"
    /// </summary>
    public string MainText { get; set; } = string.Empty;

    /// <summary>
    /// Partie secondaire (code postal + ville)
    /// Ex: "75001 Paris, France"
    /// </summary>
    public string SecondaryText { get; set; } = string.Empty;

    /// <summary>
    /// Adresse complète pour affichage
    /// </summary>
    public string FullAddress => Description;
}