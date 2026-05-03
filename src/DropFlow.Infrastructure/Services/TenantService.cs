using System.Security.Claims;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Constants;
using DropFlow.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DropFlow.Infrastructure.Services;

public class TenantService(
    IHttpContextAccessor httpContextAccessor,
    IApplicationDbContext context)
    : ITenantService
{
    public int GetTenantId()
    {
        var tenantIdClaim = httpContextAccessor.HttpContext?.User
            .FindFirst("TenantId")?.Value;

        if (tenantIdClaim == null || !int.TryParse(tenantIdClaim, out var tenantId))
        {
            throw new UnauthorizedAccessException("Tenant not found");
        }

        return tenantId;
    }

    public bool IsDropFlowAdmin()
    {
        var tenantId = GetTenantId();
        return tenantId == TenantIds.DropFlowAdmin;
    }
    
    public Tenant? GetCurrentTenant()
    {
        var tenantId = GetTenantId();
        
        // ✅ Si Admin DropFlow, pas de tenant spécifique
        if (tenantId == TenantIds.DropFlowAdmin)
            return null;
        
        return context.Tenants
                   .AsNoTracking()
                   .FirstOrDefault(t => t.Id == tenantId && t.IsActive)
               ?? throw new UnauthorizedAccessException("Tenant not active");
    }
    
    public ApplicationUser? GetCurrentUser()
    {
        var userId = httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return null;

        return context.Users
            .AsNoTracking()
            .FirstOrDefault(u => u.Id == userId);
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return null;

        return await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}