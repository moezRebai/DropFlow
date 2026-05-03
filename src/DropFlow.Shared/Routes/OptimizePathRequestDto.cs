namespace DropFlow.Shared.Routes;

public class OptimizePathRequestDto
{
    public string DepartureAddress { get; set; }
    public List<int> DeliveryIds { get; set; }
}