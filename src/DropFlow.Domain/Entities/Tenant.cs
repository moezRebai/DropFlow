namespace DropFlow.Domain.Entities;

public class Tenant
{
    // ═══════════════════════════════════════════════════════════
    // PROPRIÉTÉS EXISTANTES (NE PAS MODIFIER)
    // ═══════════════════════════════════════════════════════════
    
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string? SubDomain { get; private set; }
    public string PlanType { get; private set; }
    public int MaxUsers { get; private set; }
    public int MaxDeliveries { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    
    // ═══════════════════════════════════════════════════════════
    // ✨ NOUVELLES PROPRIÉTÉS - INFORMATIONS ENTREPRISE
    // ═══════════════════════════════════════════════════════════
    
    // Informations générales
    public string? CompanyName { get; private set; }  // Raison sociale officielle
    public string? LogoUrl { get; private set; }      // Logo entreprise (URL)
    
    // Coordonnées
    public string? Address { get; private set; }      // Adresse complète
    public string? ZipCode { get; private set; }      // Code postal
    public string? City { get; private set; }         // Ville
    public string? Phone { get; private set; }        // Téléphone
    public string? Email { get; private set; }        // Email
    public string? Website { get; private set; }      // Site web
    
    // Informations légales (France)
    public string? Siret { get; private set; }        // 14 chiffres
    public string? VatNumber { get; private set; }    // FR + 11 chiffres
    public string? LegalForm { get; private set; }    // SARL, SAS, EURL, etc.
    
    // Documents
    public string? LegalMentions { get; private set; }  // Mentions légales factures
    public string? BankDetails { get; private set; }    // IBAN/BIC
    
    // Audit
    public DateTime? ModifiedDate { get; private set; }
    
    // ═══════════════════════════════════════════════════════════
    // ✨ NAVIGATION PROPERTIES
    // ═══════════════════════════════════════════════════════════
    
    public ICollection<TenantDepot> Depots { get; private set; } = new List<TenantDepot>();
    
    // ═══════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════
    
    private Tenant() 
    {
        Name = string.Empty;
        Depots = new List<TenantDepot>();
    }
    
    // ═══════════════════════════════════════════════════════════
    // FACTORY METHOD (EXISTANT)
    // ═══════════════════════════════════════════════════════════
    
    public static Tenant Create(string name, string planType = "Free")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Company name is required", nameof(name));
        
        return new Tenant
        {
            Name = name,
            CompanyName = name,  // Par défaut, CompanyName = Name
            PlanType = planType,
            MaxUsers = 5,
            MaxDeliveries = 50,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            Depots = new List<TenantDepot>()
        };
    }
    
    // ═══════════════════════════════════════════════════════════
    // MÉTHODES EXISTANTES
    // ═══════════════════════════════════════════════════════════
    
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    
    public void UpdatePlan(string planType, int maxUsers, int maxDeliveries)
    {
        PlanType = planType;
        MaxUsers = maxUsers;
        MaxDeliveries = maxDeliveries;
        ModifiedDate = DateTime.UtcNow;
    }
    
    // ═══════════════════════════════════════════════════════════
    // ✨ NOUVELLES MÉTHODES - GESTION INFORMATIONS ENTREPRISE
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Met à jour les informations générales de l'entreprise
    /// </summary>
    public void UpdateCompanyInfo(
        string? companyName,
        string? address,
        string? zipCode,
        string? city,
        string? phone,
        string? email,
        string? website)
    {
        CompanyName = companyName;
        Address = address;
        ZipCode = zipCode;
        City = city;
        Phone = phone;
        Email = email;
        Website = website;
        ModifiedDate = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Met à jour les informations légales
    /// </summary>
    public void UpdateLegalInfo(
        string? siret,
        string? vatNumber,
        string? legalForm,
        string? legalMentions,
        string? bankDetails)
    {
        Siret = siret;
        VatNumber = vatNumber;
        LegalForm = legalForm;
        LegalMentions = legalMentions;
        BankDetails = bankDetails;
        ModifiedDate = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Met à jour le logo
    /// </summary>
    public void UpdateLogo(string? logoUrl)
    {
        LogoUrl = logoUrl;
        ModifiedDate = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Supprime le logo
    /// </summary>
    public void RemoveLogo()
    {
        LogoUrl = null;
        ModifiedDate = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Vérifie si le tenant a un logo
    /// </summary>
    public bool HasLogo() => !string.IsNullOrEmpty(LogoUrl);
    
    /// <summary>
    /// Obtient le nom d'affichage (CompanyName si existe, sinon Name)
    /// </summary>
    public string GetDisplayName() => CompanyName ?? Name;
}
