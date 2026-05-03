namespace DropFlow.Shared.Admin;

public class UpdateTenantPlanDto
{
    public string PlanType { get; set; } = "Free";
    public int MaxUsers { get; set; }
    public int MaxDeliveries { get; set; }
    public DateTime? ExpiryDate { get; set; }
}