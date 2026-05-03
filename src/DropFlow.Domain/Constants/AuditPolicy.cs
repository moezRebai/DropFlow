namespace DropFlow.Domain.Constants;

/// <summary>
/// Politique d'audit sélectif - Définit quelles actions sont auditées
/// </summary>
public static class AuditPolicy
{
    /// <summary>
    /// Actions TOUJOURS auditées (critique)
    /// </summary>
    public static readonly HashSet<string> CriticalActions = new()
    {
        // Auth
        AuditActions.Login,
        AuditActions.Logout,
        AuditActions.LoginFailed,
        
        // Users
        AuditActions.TenantCreated,
        AuditActions.UserCreated,
        AuditActions.UserInvited,
        AuditActions.InvitationAccepted,
        AuditActions.UserDeactivated,
        AuditActions.UserReactivated,
        AuditActions.RoleChanged
    };

    /// <summary>
    /// Vérifie si une action doit être auditée
    /// </summary>
    public static bool ShouldAudit(string action)
    {
        return CriticalActions.Contains(action);
    }
}