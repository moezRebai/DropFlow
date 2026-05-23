using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DropFlow.Mobile.Models;
using DropFlow.Mobile.Services;

namespace DropFlow.Mobile.ViewModels;

public partial class HistoryViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private int _currentPage = 1;
    private const int PageSize = 20;

    [ObservableProperty] private List<DeliveryHistoryItem> _deliveries = [];
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private bool _hasMore;

    public HistoryViewModel(ApiService api, ConnectivityService connectivity) : base(connectivity)
    {
        _api = api;
    }

    public async Task InitializeAsync()
    {
        _currentPage = 1;
        Deliveries = [];
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _api.GetDeliveryHistoryAsync(_currentPage, PageSize);
            TotalCount = result.TotalCount;
            Deliveries = [.. Deliveries, .. result.Deliveries];
            HasMore = Deliveries.Count < TotalCount;
        });
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!HasMore || IsBusy) return;
        _currentPage++;
        await LoadPageAsync();
    }

    [RelayCommand]
    private static async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
}
