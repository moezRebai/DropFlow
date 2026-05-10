using System.Text.Json;
using System.Text.Json.Serialization;
using DropFlow.Api.Extensions;
using DropFlow.Api.Middleware;
using DropFlow.Application;
using DropFlow.Application.Interfaces;
using DropFlow.Infrastructure;
using DropFlow.Infrastructure.Persistence;
using DropFlow.Infrastructure.Services.Geocoding;
using Microsoft.AspNetCore.RateLimiting;
using QuestPDF.Infrastructure;

namespace DropFlow.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        QuestPDF.Settings.License = LicenseType.Community;  // ✅ Ligne 2

        var builder = WebApplication.CreateBuilder(args);

        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION DES SERVICES
        // ═══════════════════════════════════════════════════════════════

        // Infrastructure (Database, Services externes)
        builder.Services.AddInfrastructureServices(builder.Configuration);

        // Application (Services métier)
        builder.Services.AddApplicationServices();

        // Authentication & Authorization
        builder.Services.AddAuthenticationServices(builder.Configuration);
        builder.Services.AddAuthorizationPolicies();

        // Security (HTTPS, HSTS)
        builder.Services.AddSecurityServices(builder.Environment);

        // Rate Limiting — auth endpoints
        builder.Services.AddRateLimiter(options =>
        {
            options.AddSlidingWindowLimiter("auth", limiterOptions =>
            {
                limiterOptions.PermitLimit = 10;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.SegmentsPerWindow = 2;
                limiterOptions.QueueLimit = 0;
            });
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        // CORS
        builder.Services.AddCorsConfiguration(builder.Configuration);

        // Swagger
        builder.Services.AddSwaggerConfiguration();

        // Controllers & API
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        // HttpContext Accessor
        builder.Services.AddHttpContextAccessor();

        // Health Checks
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database");

        builder.Services.AddHttpClient<IGeocodingService, GeocodingService>();
        builder.Services.AddHttpClient<IAddressAutocompleteService, AddressAutocompleteService>();

        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION DU PIPELINE
        // ═══════════════════════════════════════════════════════════════

        var app = builder.Build();

        // Exception Handling
        app.UseCustomExceptionHandling();

        // Swagger (Development only)
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "DropFlow API v1");
                options.RoutePrefix = "swagger";
            });
        }
        else
        {
            app.UseHsts();
        }

        // HTTPS Redirection
        app.UseHttpsRedirection();

        // Security Headers
        app.UseSecurityHeaders();

        // Request Logging
        app.UseRequestLogging();

        // Static Files (si nécessaire)
        // app.UseStaticFiles();

        // Routing
        app.UseRouting();

        // CORS
        var corsPolicy = app.Environment.IsDevelopment() ? "AllowBlazorClient" : "Production";
        app.UseCors(corsPolicy);

        // Rate Limiting
        app.UseRateLimiter();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Endpoints
        app.MapControllers();
        
        app.MapHealthChecks("/health");

        // ═══════════════════════════════════════════════════════════════
        // INITIALISATION
        // ═══════════════════════════════════════════════════════════════

        // Initialize Database & Seed Data
        await app.InitializeDatabaseAsync();

        // ═══════════════════════════════════════════════════════════════
        // DÉMARRAGE
        // ═══════════════════════════════════════════════════════════════

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("DropFlow API starting...");
        logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

        await app.RunAsync();
    }
}