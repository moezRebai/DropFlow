namespace DropFlow.Shared.Deliveries;

public class DeliveryStatsDto
{
    public int TotalCount { get; set; }
    public int ToBePlannedCount { get; set; }
    public int ConfirmedTodayCount { get; set; }
    public int PlannedCount { get; set; }
    public int InProgressCount { get; set; }
    public int DeliveredCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalClientPayment { get; set; }
    public decimal TotalStorePayment { get; set; }
}