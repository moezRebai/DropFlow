using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DropFlow.Mobile.Models;
using DropFlow.Mobile.Services;

namespace DropFlow.Mobile.ViewModels;

[QueryProperty(nameof(DeliveryId), "DeliveryId")]
public partial class DeliveryDetailViewModel : BaseViewModel
{
    private readonly ApiService _api;

    [ObservableProperty] private int _deliveryId;
    [ObservableProperty] private DeliveryDetailDto? _delivery;

    public DeliveryDetailViewModel(ApiService api, ConnectivityService connectivity)
        : base(connectivity)
    {
        _api = api;
    }

    partial void OnDeliveryIdChanged(int value)
    {
        if (value > 0)
            MainThread.BeginInvokeOnMainThread(async () => await LoadDeliveryAsync());
    }

    [RelayCommand]
    private async Task LoadDeliveryAsync()
    {
        await ExecuteAsync(async () =>
        {
            Delivery = await _api.GetDeliveryDetailAsync(DeliveryId);
        });
    }

    [RelayCommand]
    private void CallClient()
    {
        if (string.IsNullOrWhiteSpace(Delivery?.ClientPhone)) return;
        PhoneDialer.Default.Open(Delivery.ClientPhone);
    }

    [RelayCommand]
    private async Task NavigateToClientAsync()
    {
        if (Delivery is null) return;

        if (Delivery.Latitude.HasValue && Delivery.Longitude.HasValue)
        {
            var location = new Location(Delivery.Latitude.Value, Delivery.Longitude.Value);
            await Map.Default.OpenAsync(location, new MapLaunchOptions
            {
                Name = Delivery.ClientName,
                NavigationMode = NavigationMode.Driving
            });
        }
        else
        {
            var url = $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(Delivery.FullAddress)}";
            await Launcher.Default.OpenAsync(url);
        }
    }

    [RelayCommand]
    private static async Task GoBackAsync() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task ValidateDeliveryAsync()
    {
        if (Delivery is null) return;

        await Shell.Current.GoToAsync("validation", new Dictionary<string, object>
        {
            { "DeliveryId", Delivery.Id },
            { "ClientName", Delivery.ClientName },
            { "HasPayment", Delivery.HasPayment },
            { "PaymentAmount", Delivery.ClientPaymentAmount }
        });
    }
}
