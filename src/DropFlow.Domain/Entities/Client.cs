using DropFlow.Domain.Common;

namespace DropFlow.Domain.Entities;

public class Client : ITenantEntity, IAuditableEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    
    // Contact
    public string Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    
    // Relations
    public List<ClientAddress> Addresses { get; set; } = new();
    public List<Delivery> Deliveries { get; set; } = new();
    
    // Calculated
    public string DisplayName => $"{FirstName} {LastName}";
    
    // Audit
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string ModifiedBy { get; set; }
}