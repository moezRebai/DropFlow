using DropFlow.Domain.Common;

namespace DropFlow.Domain.Entities;

public class Driver : ITenantEntity
{
    public int Id { get; private set; }
    public int TenantId { get; set; }
    
    // Lien obligatoire vers User
    public string UserId { get; private set; }
    public ApplicationUser User { get; set; }
    
    // Infos métier livreur
    public string? LicenseNumber { get; private set; }
    public DateTime? LicenseExpiryDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedDate { get; private set; }
    
    // Navigation
    public List<RouteTeam> RouteAssignments { get; set; } = new();
    public List<Delivery> UrgentDeliveries { get; set; } = new();
    
    private Driver()
    {
        UserId = string.Empty;
    }
    
    public static Driver Create(
        string userId,
        string? licenseNumber = null,
        DateTime? licenseExpiryDate = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required", nameof(userId));
        
        return new Driver
        {
            UserId = userId,
            LicenseNumber = licenseNumber,
            LicenseExpiryDate = licenseExpiryDate,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };
    }
    
    public void Update(
        string? licenseNumber,
        DateTime? licenseExpiryDate)
    {
        LicenseNumber = licenseNumber;
        LicenseExpiryDate = licenseExpiryDate;
    }
    
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}