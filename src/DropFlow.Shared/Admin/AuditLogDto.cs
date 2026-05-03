namespace DropFlow.Shared.Admin;

public record AuditLogDto(
    long Id,
    int TenantId,
    string? TenantName,
    string? UserId,
    string? UserEmail,
    string Action,
    string EntityName,
    int? EntityId,
    string? Changes,
    string Severity,
    DateTime Timestamp
);