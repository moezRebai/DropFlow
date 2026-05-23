using DropFlow.Mobile.ViewModels;

namespace DropFlow.Mobile.Views;

public partial class RoutePage : ContentPage
{
    private readonly RouteViewModel _vm;

    public RoutePage(RouteViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }
}
