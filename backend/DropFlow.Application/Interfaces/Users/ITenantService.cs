using DropFlow.Domain.Entities;

namespace DropFlow.Application.Interfaces.Users;

public interface ITenantService
{
    int GetTenantId();
    Tenant? GetCurrentTenant();
    ApplicationUser? GetCurrentUser();
    Task<ApplicationUser?> GetCurrentUserAsync();
    public bool IsDropFlowAdmin();
}