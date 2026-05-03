using System.ComponentModel.DataAnnotations;

namespace DropFlow.Shared.Tenants.Depots;

/// <summary>
/// DTO pour créer un nouveau dépôt
/// </summary>
public class CreateTenantDepotDto
{
    [Required(ErrorMessage = "Le nom du dépôt est requis")]
    [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "L'adresse est requise")]
    [MaxLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
    public string FullAddress { get; set; } = string.Empty;
    
    [MaxLength(100, ErrorMessage = "La ville ne peut pas dépasser 100 caractères")]
    public string? City { get; set; }
    
    [MaxLength(10, ErrorMessage = "Le code postal ne peut pas dépasser 10 caractères")]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "Le code postal doit contenir 5 chiffres")]
    public string? ZipCode { get; set; }
    
    // Coordonnées GPS (optionnelles)
    [Range(-90, 90, ErrorMessage = "La latitude doit être entre -90 et 90")]
    public double? Latitude { get; set; }
    
    [Range(-180, 180, ErrorMessage = "La longitude doit être entre -180 et 180")]
    public double? Longitude { get; set; }
    
    // Status
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}