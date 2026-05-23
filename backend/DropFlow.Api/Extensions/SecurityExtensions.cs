namespace DropFlow.Api.Extensions;

public static class SecurityExtensions
{
    public static IServiceCollection AddSecurityServices(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        // HTTPS Redirection
        services.AddHttpsRedirection(options =>
        {
            options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
            options.HttpsPort = environment.IsDevelopment() ? 7001 : 443;
        });

        // HSTS (HTTP Strict Transport Security)
        if (!environment.IsDevelopment())
        {
            services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(365);
                options.IncludeSubDomains = true;
                options.Preload = true;
            });
        }

        return services;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            // Security Headers
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
            
            // Content Security Policy
            // 'unsafe-inline'/'unsafe-eval' removed — this is a JSON API, not a browser app.
            // Swagger UI (dev only) is served before this middleware runs.
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'none'; " +
                "script-src 'self'; " +
                "style-src 'self'; " +
                "img-src 'self'; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none';");

            // Remove server header
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await next();
        });

        return app;
    }
}