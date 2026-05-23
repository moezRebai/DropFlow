using CommunityToolkit.Maui;
using DropFlow.Mobile.Services;
using DropFlow.Mobile.ViewModels;
using DropFlow.Mobile.Views;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace DropFlow.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts => { });

        // Services
        builder.Services.AddSingleton<AuthStorageService>();
        builder.Services.AddSingleton<ConnectivityService>();
        builder.Services.AddSingleton<ApiService>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RouteViewModel>();
        builder.Services.AddTransient<DeliveryDetailViewModel>();
        builder.Services.AddTransient<ValidationViewModel>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();

        // Views
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RoutePage>();
        builder.Services.AddTransient<DeliveryDetailPage>();
        builder.Services.AddTransient<ValidationPage>();
        builder.Services.AddTransient<HistoryPage>();
        builder.Services.AddTransient<ProfilePage>();

        return builder.Build();
    }
}
