using DropFlow.Mobile.ViewModels;

namespace DropFlow.Mobile.Views;

public partial class ValidationPage : ContentPage
{
    private readonly ValidationViewModel _vm;

    public ValidationPage(ValidationViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SignaturePad.SignatureChanged += _vm.OnSignatureChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        SignaturePad.SignatureChanged -= _vm.OnSignatureChanged;
    }

    private void OnClearSignatureTapped(object? sender, EventArgs e)
    {
        SignaturePad.Clear();
    }
}
