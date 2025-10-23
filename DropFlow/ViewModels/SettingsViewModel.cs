using CommunityToolkit.Mvvm.ComponentModel;

namespace DropFlow.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _clientsCount;

    [ObservableProperty]
    private int _plannedDeliveriesCount;

    [ObservableProperty]
    private int _unplannedDeliveriesCount;

    public SettingsViewModel()
    {
        // ⚡ TODO: Replace with real data from your service / DB
        ClientsCount = 120;
        PlannedDeliveriesCount = 45;
        UnplannedDeliveriesCount = 10;
    }
}
