namespace DropFlow.Shared.Admin;

public class TenantUserDto(
    string id,
    string email,
    string firstName,
    string lastName,
    string? phoneNumber,
    string role,
    bool isActive,
    DateTime createdDate,
    DateTime? lastLoginDate,
    int tenantId,
    string tenantName)
{
    public string Id { get; set; } = id;
    public string Email { get; set; } = email;
    public string FirstName { get; set; } = firstName;
    public string LastName { get; set; } = lastName;
    public string? PhoneNumber { get; set; } = phoneNumber;
    public string Role { get; set; } = role;
    public bool IsActive { get; set; } = isActive;
    public DateTime CreatedDate { get; set; } = createdDate;
    public DateTime? LastLoginDate { get; set; } = lastLoginDate;
    public int TenantId { get; set; } = tenantId;
    public string TenantName { get; set; } = tenantName;

    public string FullName => $"{FirstName} {LastName}".Trim();
}
