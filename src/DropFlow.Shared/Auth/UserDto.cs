namespace DropFlow.Shared.Auth;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Role { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public string? TenantName { get; set; }
    public bool IsActive { get; set; }
}