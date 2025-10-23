using CommunityToolkit.Mvvm.ComponentModel;

namespace DropFlow.ViewModels;

public partial class InvoicesViewModel : ObservableObject
{
    [ObservableProperty]
    private int _clientsCount;

    [ObservableProperty]
    private int _plannedDeliveriesCount;

    [ObservableProperty]
    private int _unplannedDeliveriesCount;

    public InvoicesViewModel()
    {
        // ⚡ TODO: Replace with real data from your service / DB
        ClientsCount = 120;
        PlannedDeliveriesCount = 45;
        UnplannedDeliveriesCount = 10;
    }
}
