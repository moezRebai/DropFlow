using CommunityToolkit.Mvvm.ComponentModel;

namespace DropFlow.ViewModels;

public partial class StockViewModel : ObservableObject
{
    [ObservableProperty]
    private int _clientsCount;

    [ObservableProperty]
    private int _plannedDeliveriesCount;

    [ObservableProperty]
    private int _unplannedDeliveriesCount;

    public StockViewModel()
    {
        // ⚡ TODO: Replace with real data from your service / DB
        ClientsCount = 120;
        PlannedDeliveriesCount = 45;
        UnplannedDeliveriesCount = 10;
    }
}
