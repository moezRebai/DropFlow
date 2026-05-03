using System.Text.Json;
using System.Text.Json.Serialization;
using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Constants;
using DropFlow.Domain.Entities;
using DropFlow.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DropFlow.Infrastructure.Services;

public class AuditService(
    IApplicationDbContext context,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditService> logger)
    : IAuditService
{
    public async Task LogAsync(
        int tenantId,
        string? userId,
        string action,
        string entityName,
        int? entityId = null,
        object? changes = null,
        AuditSeverity severity = AuditSeverity.Info)
    {
        try
        {
            // ✅ Audit sélectif
            if (!AuditPolicy.ShouldAudit(action))
            {
                logger.LogDebug("Action {Action} not audited (selective policy)", action);
                return;
            }

            var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            string? changesJson = null;
            if (changes != null)
            {
                changesJson = JsonSerializer.Serialize(changes, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            }

            var audit = AuditLog.Create(
                tenantId,
                userId,
                action,
                entityName,
                entityId,
                changesJson,
                ipAddress,
                severity);

            context.AuditLogs.Add(audit);
            await context.SaveChangesAsync();

            logger.LogInformation(
                "Audit logged: {Action} on {Entity} by {User}",
                action, entityName, userId ?? "System");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log audit for action {Action}", action);
        }
    }
}