using System.ComponentModel.DataAnnotations;

namespace DropFlow.Shared.Tenants;

/// <summary>
/// DTO pour mettre à jour les informations générales de l'entreprise
/// </summary>
public class UpdateTenantCompanyInfoDto
{
    [MaxLength(200, ErrorMessage = "Le nom de l'entreprise ne peut pas dépasser 200 caractères")]
    public string? CompanyName { get; set; }
    
    [MaxLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
    public string? Address { get; set; }
    
    [MaxLength(10, ErrorMessage = "Le code postal ne peut pas dépasser 10 caractères")]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "Le code postal doit contenir 5 chiffres")]
    public string? ZipCode { get; set; }
    
    [MaxLength(100, ErrorMessage = "La ville ne peut pas dépasser 100 caractères")]
    public string? City { get; set; }
    
    [MaxLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
    [Phone(ErrorMessage = "Le format du téléphone est invalide")]
    public string? Phone { get; set; }
    
    [MaxLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
    [EmailAddress(ErrorMessage = "Le format de l'email est invalide")]
    public string? Email { get; set; }
    
    [MaxLength(200, ErrorMessage = "Le site web ne peut pas dépasser 200 caractères")]
    [Url(ErrorMessage = "Le format du site web est invalide")]
    public string? Website { get; set; }
}