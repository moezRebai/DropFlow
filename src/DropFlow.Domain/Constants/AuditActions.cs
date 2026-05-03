namespace DropFlow.Domain.Constants;

public static class AuditActions
{
    // Auth
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string LoginFailed = "LoginFailed";
    
    // Tenant & Users
    public const string TenantCreated = "TenantCreated";
    public const string UserCreated = "UserCreated";
    public const string UserInvited = "UserInvited";
    public const string InvitationAccepted = "InvitationAccepted";
    public const string UserDeactivated = "UserDeactivated";
    public const string UserReactivated = "UserReactivated";
    public const string RoleChanged = "RoleChanged";
}