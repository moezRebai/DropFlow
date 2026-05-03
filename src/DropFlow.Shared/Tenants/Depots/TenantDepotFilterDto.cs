// ═══════════════════════════════════════════════════════════════════
// ✨ AJOUTER CES PROPRIÉTÉS DANS TenantDepotFilterDto (Step3_TenantDepotDtos.cs)
// SI tu ne les as pas déjà ajoutées
// ═══════════════════════════════════════════════════════════════════

namespace DropFlow.Shared.Tenants.Depots;

public class TenantDepotFilterDto
{
    /// <summary>
    /// Recherche par nom ou adresse
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// Filtrer par statut actif/inactif
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Filtrer uniquement le dépôt par défaut
    /// </summary>
    public bool? IsDefault { get; set; }
    
    /// <summary>
    /// Filtrer par ville
    /// </summary>
    public string? City { get; set; }
    
    /// <summary>
    /// Filtrer par code postal
    /// </summary>
    public string? ZipCode { get; set; }
    
    // ✨ AJOUTER CES 2 PROPRIÉTÉS POUR LA PAGINATION
    /// <summary>
    /// Numéro de page (commence à 1)
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// Nombre d'éléments par page
    /// </summary>
    public int PageSize { get; set; } = 10;
}