using DropFlow.Domain.Common;

namespace DropFlow.Domain.Entities;

public class Store : ITenantEntity, IAuditableEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    
    // Identification
    public string Name { get; set; } // Ex: "Conforama Paris 15" (Unique par tenant)
    
    // Address
    public string Address { get; set; }
    public string ZipCode { get; set; }
    public string City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // Contact
    public string ContactName { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    
    // Notes
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    
    // Relations
    public List<Delivery> Deliveries { get; set; } = new();
    
    // Audit
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string ModifiedBy { get; set; }
}