namespace DropFlow.WebApp.Models.Audit;

public class AuditFilters
{
    public int? TenantId { get; set; }
    public string? Action { get; set; }
    public string? Severity { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}