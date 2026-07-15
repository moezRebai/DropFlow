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
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Serilog;
using Serilog.Events;

namespace DropFlow.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            QuestPDF.Settings.License = LicenseType.Community;

            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());

            // ═══════════════════════════════════════════════════════════════
            // CONFIGURATION DES SERVICES
            // ═══════════════════════════════════════════════════════════════

            // Infrastructure (Database, Services externes)
            builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

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

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var cs = db.Database.GetConnectionString() ?? string.Empty;
                var host = cs.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim().Split('=', 2))
                    .Where(p => p.Length == 2 && p[0].Equals("Host", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p[1])
                    .FirstOrDefault() ?? "unknown";
                var isNeon = host.Contains("neon.tech", StringComparison.OrdinalIgnoreCase);

                Log.Information("Database connected: {Label} (host={Host}, environment={Environment})",
                    isNeon ? "Neon" : "Local", host, app.Environment.EnvironmentName);
            }

            // Exception Handling
            app.UseCustomExceptionHandling();

            // Swagger (Development only)
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "DropFlow API v1");
                options.RoutePrefix = "swagger";
            });

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            // Security Headers
            app.UseSecurityHeaders();

            // Request Logging
            app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = (HttpContext ctx, double _, Exception? ex) =>
                    ex != null || ctx.Response.StatusCode >= 500 ? LogEventLevel.Error
                    : ctx.Response.StatusCode >= 400 ? LogEventLevel.Warning
                    : LogEventLevel.Information;
            });

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

            Log.Information("DropFlow API starting on {Environment}", app.Environment.EnvironmentName);

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}