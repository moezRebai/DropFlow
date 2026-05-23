using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Deliveries;
using DropFlow.Application.Interfaces.Drivers;
using DropFlow.Application.Interfaces.Emails;
using DropFlow.Application.Interfaces.Routes;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Application.Services;
using DropFlow.Domain.Configurations;
using DropFlow.Infrastructure.Persistence;
using DropFlow.Infrastructure.Services;
using DropFlow.Infrastructure.Services.Email;
using DropFlow.Infrastructure.Services.Email.Templates;
using DropFlow.Infrastructure.Services.Pdf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DropFlow.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("NeonConnection"),
                b => b.MigrationsAssembly("DropFlow.Infrastructure")));

        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        
        // Register IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Infrastructure Services
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IDeliveryReferenceService, DeliveryReferenceService>();
        services.AddScoped<IRouteReferenceService, RouteReferenceService>();
        services.AddScoped<ITimeSlotService, TimeSlotService>();
        services.AddHttpClient("PdfImages", c => c.Timeout = TimeSpan.FromSeconds(10));
        services.AddScoped<IRouteSheetPdfGenerator, RouteSheetPdfGenerator>();
        services.AddSingleton<IFileStorageService, FileStorageService>();
        services.AddMemoryCache();
        services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
        services.AddSingleton<IAppCacheService, AppCacheService>();
    }
}