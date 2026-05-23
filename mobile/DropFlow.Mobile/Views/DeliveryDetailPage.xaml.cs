using DropFlow.Mobile.ViewModels;

namespace DropFlow.Mobile.Views;

public partial class DeliveryDetailPage : ContentPage
{
    public DeliveryDetailPage(DeliveryDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
