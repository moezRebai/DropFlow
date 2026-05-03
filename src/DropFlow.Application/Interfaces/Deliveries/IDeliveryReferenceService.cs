namespace DropFlow.Application.Interfaces.Deliveries;

public interface IDeliveryReferenceService
{
    Task<string> GenerateReferenceAsync(int tenantId, DateTime? date = null);
    Task<int> GetNextSequentialNumberAsync(int tenantId, DateTime? date = null);
}