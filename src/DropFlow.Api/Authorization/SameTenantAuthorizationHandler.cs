using DropFlow.Api.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace DropFlow.Api.Authorization;

public class SameTenantAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<SameTenantRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameTenantRequirement requirement)
    {
        var userTenantId = context.User.FindFirst("TenantId")?.Value;
        
        // Récupérer le tenantId de la route ou du body
        var httpContext = httpContextAccessor.HttpContext;
        var routeTenantId = httpContext?.Request.RouteValues["tenantId"]?.ToString();

        if (userTenantId != null && userTenantId == routeTenantId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}