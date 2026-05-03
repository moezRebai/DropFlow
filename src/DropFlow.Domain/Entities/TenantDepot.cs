namespace DropFlow.Domain.Entities;

/// <summary>
/// Représente un dépôt (entrepôt/point de départ) d'un tenant
/// Utilisé pour les tournées de livraison
/// </summary>
public class TenantDepot
{
    // ═══════════════════════════════════════════════════════════
    // PROPRIÉTÉS
    // ═══════════════════════════════════════════════════════════
    
    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    
    // Informations du dépôt
    public string Name { get; private set; } = string.Empty;         // Ex: "Dépôt Vélizy"
    public string FullAddress { get; private set; } = string.Empty;  // Adresse complète
    public string? City { get; private set; }
    public string? ZipCode { get; private set; }
    
    // Coordonnées GPS (pour optimisation des routes)
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    
    // Status
    public bool IsDefault { get; private set; }  // Un seul dépôt par défaut par tenant
    public bool IsActive { get; private set; }   // Actif/Inactif
    
    // Audit
    public DateTime CreatedDate { get; private set; }
    public DateTime? ModifiedDate { get; private set; }
    
    // ═══════════════════════════════════════════════════════════
    // CONSTRUCTOR (privé pour EF Core)
    // ═══════════════════════════════════════════════════════════
    
    private TenantDepot() { }
    
    // ═══════════════════════════════════════════════════════════
    // FACTORY METHOD - CRÉATION
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Crée un nouveau dépôt
    /// </summary>
    public static TenantDepot Create(
        int tenantId,
        string name,
        string fullAddress,
        string? city = null,
        string? zipCode = null,
        double? latitude = null,
        double? longitude = null,
        bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Depot name is required", nameof(name));
        
        if (string.IsNullOrWhiteSpace(fullAddress))
            throw new ArgumentException("Depot address is required", nameof(fullAddress));
        
        return new TenantDepot
        {
            TenantId = tenantId,
            Name = name,
            FullAddress = fullAddress,
            City = city,
            ZipCode = zipCode,
            Latitude = latitude,
            Longitude = longitude,
            IsDefault = isDefault,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
    }
    
    // ═══════════════════════════════════════════════════════════
    // MÉTHODES DE MISE À JOUR
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Met à jour les informations du dépôt
    /// </summary>
    public void Update(
        string name,
        string fullAddress,
        string? city,
        string? zipCode,
        double? latitude,
        double? longitude)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Depot name is required", nameof(name));
        
        if (string.IsNullOrWhiteSpace(fullAddress))
            throw new ArgumentException("Depot address is required", nameof(fullAddress));
        
        Name = name;
        FullAddress = fullAddress;
        City = city;
        ZipCode = zipCode;
        Latitude = latitude;
        Longitude = longitude;
        ModifiedDate = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Définit ce dépôt comme par défaut
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        ModifiedDate = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Retire le statut par défaut
    /// </summary>
    public void UnsetAsDefault()
    {
        IsDefault = false;
        ModifiedDate = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Active le dépôt
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        ModifiedDate = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Désactive le dépôt
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        ModifiedDate = DateTime.UtcNow;
    }
}
