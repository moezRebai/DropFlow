namespace DropFlow.Application.Interfaces.Routes;

public interface IRouteReferenceService
{
    Task<string> GenerateReferenceAsync(int tenantId, DateTime date);
}