using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using MudBlazor.Services;
using DropFlow.WebApp.Components;
using DropFlow.WebApp.Interfaces;
using DropFlow.WebApp.Interfaces.Caches;
using DropFlow.WebApp.Providers;
using DropFlow.WebApp.Services;
using DropFlow.WebApp.Services.Cache;
using DropFlow.WebApp.Services.Users;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var culture = new CultureInfo("fr-FR");
    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    var keysPath = builder.Configuration["DataProtection:KeysPath"]
        ?? Path.Combine(builder.Environment.ContentRootPath, "dataprotection-keys");
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("DropFlow");

// MudBlazor
    builder.Services.AddMudServices();

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddAuthorizationCore();
    builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
    builder.Services.AddScoped<JwtAuthenticationStateProvider>();
    builder.Services.AddCascadingAuthenticationState();

    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ICacheService, CacheService>();

    builder.Services.AddScoped<IProfileService, ProfileService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserManagementService, UserManagementService>();
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<IDeliveryService, DeliveryService>();
    builder.Services.AddScoped<IClientService, ClientService>();
    builder.Services.AddScoped<IStoreService, StoreService>();
    builder.Services.AddScoped<IVehicleService, VehicleService>();
    builder.Services.AddScoped<IDriverService, DriverService>();
    builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
    builder.Services.AddScoped<IRouteService, RouteService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<ITenantManagementService, TenantManagementService>();
    builder.Services.AddScoped<SystemInfoService>();
    builder.Services.AddSingleton<IDeliveryEventBus, DeliveryEventBus>(); // ✅

// HTTP Clients
    builder.Services.AddHttpClient("DropFlowAPI", client =>
    {
        var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001/";
        client.BaseAddress = new Uri(apiBaseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "DropFlow-WebApp/1.0");
    });

// CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("https://localhost:7001")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();
    app.UseCors();
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

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