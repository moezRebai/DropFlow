using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DropFlow.Mobile.Models;
using DropFlow.Mobile.Services;

namespace DropFlow.Mobile.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly AuthStorageService _auth;
    private readonly ApiService _api;

    [ObservableProperty] private UserInfo? _user;
    [ObservableProperty] private int _totalDelivered;
    [ObservableProperty] private int _totalCanceled;

    public ProfileViewModel(AuthStorageService auth, ApiService api, ConnectivityService connectivity)
        : base(connectivity)
    {
        _auth = auth;
        _api = api;
    }

    public async Task InitializeAsync()
    {
        User = await _auth.GetUserInfoAsync();
        await LoadStatsAsync();
    }

    private async Task LoadStatsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var page = 1;
            var delivered = 0;
            var canceled = 0;

            while (true)
            {
                var result = await _api.GetDeliveryHistoryAsync(page, 50);
                delivered += result.Deliveries.Count(d => d.Status == 3);
                canceled += result.Deliveries.Count(d => d.Status == 4);

                if (result.Deliveries.Count < 50 || delivered + canceled >= result.TotalCount)
                    break;
                page++;
            }

            TotalDelivered = delivered;
            TotalCanceled = canceled;
        });
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert("Déconnexion", "Voulez-vous vous déconnecter ?", "Oui", "Non");
        if (!confirmed) return;
        _auth.Clear();
        await Shell.Current.GoToAsync("//login");
    }

    [RelayCommand]
    private static async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}
