using DropFlow.Domain.Common;

public abstract class BaseEntity : ITenantEntity, IAuditableEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
}