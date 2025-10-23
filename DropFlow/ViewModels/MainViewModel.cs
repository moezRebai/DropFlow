using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace DropFlow.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private NavigationItem _selectedItem;
    
    [ObservableProperty]
    private object _currentViewModel = default!;

    [ObservableProperty]
    private ObservableCollection<NavigationItem> _items = [];

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _items.Add(new NavigationItem { Title = "Accueil", Icon = "Home", ViewKey = "Home", ViewModelType = typeof(HomeViewModel) });
        _items.Add(new NavigationItem { Title = "Livraisons", Icon = "TruckDeliveryOutline",
            ViewKey = "Deliveries" , ViewModelType = typeof(DeliveriesViewModel)});
        _items.Add(new NavigationItem { Title = "Stock", Icon = "BoxLocation", ViewKey = "Stock" , ViewModelType = typeof(StockViewModel)});
        _items.Add(new NavigationItem { Title = "Clients", Icon = "Users", ViewKey = "Clients" , ViewModelType = typeof(ClientsViewModel)});
        _items.Add(new NavigationItem { Title = "Factures", Icon = "InvoiceList", ViewKey = "Invoices", ViewModelType = typeof(InvoicesViewModel) });
        _items.Add(new NavigationItem { Title = "Paramètres", Icon = "Cog", ViewKey = "Settings" , ViewModelType = typeof(SettingsViewModel)});

        SelectedItem = _items[0];
    }
    
    partial void OnSelectedItemChanged(NavigationItem value)
    {
        if (value?.ViewModelType != null)
        {
            CurrentViewModel = _serviceProvider.GetRequiredService(value.ViewModelType);
        }
    }

}