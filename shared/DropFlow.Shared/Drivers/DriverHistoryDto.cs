namespace DropFlow.Shared.Drivers;

public class DriverHistoryDeliveryDto
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public bool IsClientAbsent { get; set; }
    public DateTime? DeliveredDateTime { get; set; }
    public string RouteReference { get; set; } = string.Empty;
}

public class DriverHistoryResponse
{
    public List<DriverHistoryDeliveryDto> Deliveries { get; set; } = [];
    public int TotalCount { get; set; }
}
