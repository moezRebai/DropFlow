using DropFlow.Shared.Enums;
using DropFlow.Domain.Enums;

namespace DropFlow.Application.Interfaces.Users;

public interface IAuditService
{
    Task LogAsync(
        int tenantId,
        string? userId,
        string action,
        string entityName,
        int? entityId = null,
        object? changes = null,
        AuditSeverity severity = AuditSeverity.Info);
}