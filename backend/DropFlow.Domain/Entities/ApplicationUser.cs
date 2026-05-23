using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace DropFlow.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public int TenantId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedDate { get; private set; }
    public DateTime? LastLoginDate { get; private set; }
    
    public DateTime? DeletedDate { get; private set; }

    // Ajouter navigation inverse
    public Driver? Driver { get; set; }
    
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
    
    [NotMapped]
    public bool IsDeleted => DeletedDate.HasValue;
    private ApplicationUser() 
    {
        FirstName = string.Empty;
        LastName = string.Empty;
    }

    public static ApplicationUser Create(
        int tenantId,
        string email,
        string firstName,
        string lastName)
    {
        if (tenantId < 0)
            throw new ArgumentException("TenantId cannot be negative", nameof(tenantId));
        
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        return new ApplicationUser
        {
            TenantId = tenantId,
            Email = email,
            UserName = $"{email}_tenant_{tenantId}",
            FirstName = firstName ?? string.Empty,
            LastName = lastName ?? string.Empty,
            IsActive = true,
            EmailConfirmed = true,
            CreatedDate = DateTime.UtcNow
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void UpdateLastLogin() => LastLoginDate = DateTime.UtcNow;
    public void UpdateProfile(string firstName, string lastName, string? phone, string? address)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phone;
        Address = address;
    }
    
    public void SoftDelete()
    {
        IsActive = false;
        DeletedDate = DateTime.UtcNow;
    }
    
    public void Restore()
    {
        IsActive = true;
        DeletedDate = null;
    }
}