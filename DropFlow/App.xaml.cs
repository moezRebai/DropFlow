using System.Windows;
using DropFlow.ViewModels;
using DropFlow.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DropFlow;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Setup DI container
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        // Resolve and show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register ViewModels
        services.AddSingleton<ViewModels.MainViewModel>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<DeliveriesViewModel>();
        services.AddSingleton<StockViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<ClientsViewModel>();
        services.AddSingleton<InvoicesViewModel>();

        // Register Windows
        services.AddTransient<MainWindow>();
        services.AddTransient<HomeView>();
    }
}