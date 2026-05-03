using System.ComponentModel.DataAnnotations;

namespace DropFlow.Shared.Tenants;

/// <summary>
/// DTO pour mettre à jour les informations légales
/// </summary>
public class UpdateTenantLegalInfoDto
{
    [MaxLength(14, ErrorMessage = "Le SIRET doit contenir 14 chiffres")]
    [RegularExpression(@"^\d{14}$", ErrorMessage = "Le SIRET doit contenir exactement 14 chiffres")]
    public string? Siret { get; set; }
    
    [MaxLength(13, ErrorMessage = "Le numéro de TVA ne peut pas dépasser 13 caractères")]
    [RegularExpression(@"^FR\d{11}$", ErrorMessage = "Le numéro de TVA doit être au format FR + 11 chiffres")]
    public string? VatNumber { get; set; }
    
    [MaxLength(50, ErrorMessage = "La forme juridique ne peut pas dépasser 50 caractères")]
    public string? LegalForm { get; set; }
    
    [MaxLength(2000, ErrorMessage = "Les mentions légales ne peuvent pas dépasser 2000 caractères")]
    public string? LegalMentions { get; set; }
    
    [MaxLength(500, ErrorMessage = "Les coordonnées bancaires ne peuvent pas dépasser 500 caractères")]
    public string? BankDetails { get; set; }
}