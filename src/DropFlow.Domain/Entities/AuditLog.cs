using DropFlow.Domain.Enums;

namespace DropFlow.Domain.Entities;

public class AuditLog
{
    public long Id { get; private set; }
    public int TenantId { get; private set; }
    public string? UserId { get; private set; }
    public string Action { get; private set; }
    public string EntityName { get; private set; }
    public int? EntityId { get; private set; }
    public string? Changes { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime Timestamp { get; private set; }
    public AuditSeverity Severity { get; private set; }

    private AuditLog() 
    {
        Action = string.Empty;
        EntityName = string.Empty;
    }

    public static AuditLog Create(
        int tenantId,
        string? userId,
        string action,
        string entityName,
        int? entityId = null,
        string? changes = null,
        string? ipAddress = null,
        AuditSeverity severity = AuditSeverity.Info)
    {
        return new AuditLog
        {
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Changes = changes,
            IpAddress = ipAddress,
            Severity = severity,
            Timestamp = DateTime.UtcNow
        };
    }
}