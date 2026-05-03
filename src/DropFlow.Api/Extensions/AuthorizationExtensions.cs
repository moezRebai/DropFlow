using DropFlow.Api.Authorization;
using DropFlow.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace DropFlow.Api.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Politique: Requiert le rôle Manager ou Admin
            options.AddPolicy("RequireManager", policy =>
                policy.RequireRole(Roles.Manager, Roles.Admin));

            // Politique: Requiert le rôle Admin uniquement
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole(Roles.Admin));

            // Politique: Utilisateur actif
            options.AddPolicy("ActiveUser", policy =>
                policy.RequireClaim("IsActive", "True"));

            // Politique: Manager actif
            options.AddPolicy("ActiveManager", policy =>
            {
                policy.RequireRole(Roles.Manager, Roles.Admin);
                policy.RequireClaim("IsActive", "True");
            });

            // Politique: Même tenant (pour les opérations inter-utilisateurs)
            options.AddPolicy("SameTenant", policy =>
                policy.Requirements.Add(new SameTenantRequirement()));
        });

        // Enregistrer le handler pour SameTenant
        services.AddScoped<IAuthorizationHandler, SameTenantAuthorizationHandler>();

        return services;
    }
}

public class SameTenantRequirement : IAuthorizationRequirement;