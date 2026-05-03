using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Deliveries;
using DropFlow.Application.Interfaces.Drivers;
using DropFlow.Application.Interfaces.Emails;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Application.Services;
using DropFlow.Application.Services.Deliveries;
using DropFlow.Application.Services.Drivers;
using DropFlow.Application.Services.Emails;
using DropFlow.Application.Services.Routes;
using DropFlow.Application.Services.Stores;
using DropFlow.Application.Services.Tenants;
using DropFlow.Application.Services.Users;
using DropFlow.Application.Services.Vehicles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DropFlow.Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IAdminService, AdminService>(); 
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IDeliveryService, DeliveryService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IDriverService, DriverService>();
        services.AddScoped<IDriverAvailabilityService, DriverAvailabilityService>();
        services.AddScoped<IRouteService, RouteService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITenantManagementService, TenantManagementService>();
        services.AddScoped<IDriverAppService, DriverAppService>();
    }
}