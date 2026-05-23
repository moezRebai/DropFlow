namespace DropFlow.Shared.Tenants;

/// <summary>
/// DTO pour afficher les informations complètes du tenant
/// </summary>
public class TenantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SubDomain { get; set; }
    public string PlanType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    
    // Informations générales
    public string? CompanyName { get; set; }
    public string? LogoUrl { get; set; }
    public string? Address { get; set; }
    public string? ZipCode { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    
    // Informations légales
    public string? Siret { get; set; }
    public string? VatNumber { get; set; }
    public string? LegalForm { get; set; }
    public string? LegalMentions { get; set; }
    public string? BankDetails { get; set; }
    
    // Audit
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    
    /// <summary>
    /// Nom d'affichage (CompanyName si existe, sinon Name)
    /// </summary>
    public string DisplayName => CompanyName ?? Name;
    
    /// <summary>
    /// Vérifie si le tenant a un logo
    /// </summary>
    public bool HasLogo => !string.IsNullOrEmpty(LogoUrl);
}