namespace DropFlow.WebApp.Models.Audit;

public class UserFilters
{
    public int? TenantId { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
}