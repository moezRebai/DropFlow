namespace DropFlow.Shared.Admin;

public class TenantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SubDomain { get; set; }
    public string PlanType { get; set; } = "Free"; // Free, Starter, Business, Enterprise
    public int MaxUsers { get; set; }
    public int MaxDeliveries { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int UserCount { get; set; }
    public int ActiveUserCount { get; set; }
    public DateTime? LastActivityDate { get; set; }
}