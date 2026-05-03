namespace DropFlow.Domain.Entities;

public class UserInvitation
{
    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public string Email { get; private set; }
    public string Role { get; private set; }
    public string Token { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public string InvitedBy { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public virtual Tenant Tenant { get; private set; } = null!;
    private UserInvitation() 
    {
        Email = string.Empty;
        Role = string.Empty;
        Token = string.Empty;
        InvitedBy = string.Empty;
    }
    public static UserInvitation Create(
        int tenantId,
        string email,
        string role,
        string invitedBy,
        int expirationHours = 72)
    {
        return new UserInvitation
        {
            TenantId = tenantId,
            Email = email,
            Role = role,
            Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            ExpiresAt = DateTime.UtcNow.AddHours(expirationHours),
            IsUsed = false,
            InvitedBy = invitedBy,
            CreatedDate = DateTime.UtcNow
        };
    }

    public bool IsValid() => !IsUsed && ExpiresAt > DateTime.UtcNow;
    public void MarkAsUsed() => IsUsed = true;
}