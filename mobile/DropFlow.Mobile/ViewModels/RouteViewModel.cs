using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DropFlow.Mobile.Models;
using DropFlow.Mobile.Services;

namespace DropFlow.Mobile.ViewModels;

public partial class RouteViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStorageService _auth;

    [ObservableProperty] private TodayRouteResponse? _todayRoute;
    [ObservableProperty] private UserInfo? _currentUser;
    [ObservableProperty] private List<RouteSummaryItem> _upcomingRoutes = [];
    [ObservableProperty] private bool _hasUpcomingRoutes;
    [ObservableProperty] private bool _showBackButton;

    // Vrai uniquement si la tournée est confirmée ET prévue aujourd'hui
    public bool CanStartRoute
    {
        get
        {
            if (TodayRoute?.Route is null) return false;
            if (TodayRoute.Route.Status != 1) return false; // 1 = Confirmed
            return DateTime.TryParse(TodayRoute.Route.Date.ToString(), out var d)
                   && d.Date == DateTime.Today;
        }
    }

    partial void OnTodayRouteChanged(TodayRouteResponse? value)
        => OnPropertyChanged(nameof(CanStartRoute));

    public RouteViewModel(ApiService api, AuthStorageService auth, ConnectivityService connectivity)
        : base(connectivity)
    {
        _api = api;
        _auth = auth;
    }

    public async Task InitializeAsync()
    {
        CurrentUser = await _auth.GetUserInfoAsync();
        await LoadRouteAsync();
    }

    [RelayCommand]
    private async Task LoadRouteAsync()
    {
        await ExecuteAsync(async () =>
        {
            var dashboard = await _api.GetDashboardAsync();
            TodayRoute = dashboard.TodayRoute;
            if (!TodayRoute.HasRoute)
            {
                UpcomingRoutes = dashboard.UpcomingRoutes;
                HasUpcomingRoutes = UpcomingRoutes.Count > 0;
            }
            else
            {
                UpcomingRoutes = [];
                HasUpcomingRoutes = false;
            }
        });
    }

    [RelayCommand]
    private async Task SelectUpcomingRouteAsync(RouteSummaryItem route)
    {
        await ExecuteAsync(async () =>
        {
            TodayRoute = await _api.GetRouteDetailAsync(route.RouteId);
            UpcomingRoutes = [];
            HasUpcomingRoutes = false;
            ShowBackButton = true;
        });
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        ShowBackButton = false;
        await LoadRouteAsync();
    }

    [RelayCommand]
    private async Task StartRouteAsync()
    {
        if (TodayRoute?.Route is null) return;

        // Validation : uniquement aujourd'hui
        if (!CanStartRoute)
        {
            await Shell.Current.DisplayAlert(
                "Impossible",
                "Vous ne pouvez démarrer qu'une tournée prévue pour aujourd'hui.",
                "OK");
            return;
        }

        var confirmed = await Shell.Current.DisplayAlert(
            "Démarrer la tournée",
            $"Voulez-vous démarrer la tournée {TodayRoute.Route.Reference} ?",
            "Démarrer", "Annuler");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            await _api.StartRouteAsync(TodayRoute.Route.RouteId);
            TodayRoute = (await _api.GetDashboardAsync()).TodayRoute;
        });
    }

    [RelayCommand]
    private async Task CompleteRouteAsync()
    {
        if (TodayRoute?.Route is null) return;

        // Validation : toutes les livraisons doivent être traitées (Delivered=3 ou Canceled=4)
        var pending = TodayRoute.Route.Deliveries.Count(d => d.Status != 3 && d.Status != 4);
        if (pending > 0)
        {
            await Shell.Current.DisplayAlert(
                "Impossible de terminer",
                $"{pending} livraison(s) encore en cours.\nChaque livraison doit être validée ou annulée avant de terminer la tournée.",
                "OK");
            return;
        }

        var confirmed = await Shell.Current.DisplayAlert(
            "Terminer la tournée",
            $"Voulez-vous terminer la tournée {TodayRoute.Route.Reference} ?",
            "Terminer", "Annuler");

        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            await _api.CompleteRouteAsync(TodayRoute.Route.RouteId);
            TodayRoute = (await _api.GetDashboardAsync()).TodayRoute;
        });
    }

    [RelayCommand]
    private async Task NavigateToDeliveryAsync(DeliveryListItem delivery)
    {
        await Shell.Current.GoToAsync("delivery", new Dictionary<string, object>
        {
            { "DeliveryId", delivery.Id }
        });
    }

    [RelayCommand]
    private static async Task NavigateToHistoryAsync()
        => await Shell.Current.GoToAsync("history");

    [RelayCommand]
    private static async Task NavigateToProfileAsync()
        => await Shell.Current.GoToAsync("profile");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert("Déconnexion", "Voulez-vous vous déconnecter ?", "Oui", "Non");
        if (!confirmed) return;

        _auth.Clear();
        await Shell.Current.GoToAsync("//login");
    }
}
