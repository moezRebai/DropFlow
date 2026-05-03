using System.ComponentModel.DataAnnotations;

namespace DropFlow.Shared.Tenants;

/// <summary>
/// DTO pour mettre à jour le logo
/// </summary>
public class UpdateTenantLogoDto
{
    [Required(ErrorMessage = "L'URL du logo est requise")]
    [MaxLength(500, ErrorMessage = "L'URL du logo ne peut pas dépasser 500 caractères")]
    [Url(ErrorMessage = "Le format de l'URL est invalide")]
    public string LogoUrl { get; set; } = string.Empty;
}