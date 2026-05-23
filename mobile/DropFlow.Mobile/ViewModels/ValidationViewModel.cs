using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DropFlow.Mobile.Models;
using DropFlow.Mobile.Services;

namespace DropFlow.Mobile.ViewModels;

[QueryProperty(nameof(DeliveryId), "DeliveryId")]
[QueryProperty(nameof(ClientName), "ClientName")]
[QueryProperty(nameof(HasPayment), "HasPayment")]
[QueryProperty(nameof(PaymentAmount), "PaymentAmount")]
public partial class ValidationViewModel : BaseViewModel
{
    private readonly ApiService _api;

    [ObservableProperty] private int _deliveryId;
    [ObservableProperty] private string _clientName = string.Empty;
    [ObservableProperty] private bool _hasPayment;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private bool _isClientAbsent;
    [ObservableProperty] private string? _signatureBase64;
    [ObservableProperty] private string? _photoBase64;
    [ObservableProperty] private string? _comment;
    [ObservableProperty] private ImageSource? _photoPreview;

    public bool CanSubmit => IsClientAbsent || SignatureBase64 is not null;

    public ValidationViewModel(ApiService api, ConnectivityService connectivity)
        : base(connectivity)
    {
        _api = api;
    }

    partial void OnIsClientAbsentChanged(bool value) => OnPropertyChanged(nameof(CanSubmit));
    partial void OnSignatureBase64Changed(string? value) => OnPropertyChanged(nameof(CanSubmit));

    public void OnSignatureChanged(string? base64)
    {
        SignatureBase64 = base64;
    }

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        var photo = await MediaPicker.Default.CapturePhotoAsync();
        if (photo is null) return;

        await using var stream = await photo.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var bytes = ms.ToArray();
        PhotoBase64 = Convert.ToBase64String(bytes);
        PhotoPreview = ImageSource.FromStream(() => new MemoryStream(bytes));
    }

    [RelayCommand]
    private void RemovePhoto()
    {
        PhotoBase64 = null;
        PhotoPreview = null;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanSubmit) return;

        var message = HasPayment
            ? $"Confirmer la livraison et l'encaissement de {PaymentAmount:F2}€ ?"
            : "Confirmer la livraison ?";

        var confirmed = await Shell.Current.DisplayAlert("Confirmation", message, "Confirmer", "Annuler");
        if (!confirmed) return;

        await ExecuteAsync(async () =>
        {
            await _api.ValidateDeliveryAsync(DeliveryId, new ValidationRequest
            {
                SignatureBase64 = SignatureBase64,
                PhotoBase64 = PhotoBase64,
                Comment = Comment,
                IsClientAbsent = IsClientAbsent
            });

            _api.InvalidateDashboardCache();
            await Shell.Current.DisplayAlert("Succès", "Livraison validée avec succès ✅", "OK");
            await Shell.Current.GoToAsync("../..");
        });
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        var hasData = SignatureBase64 is not null || PhotoBase64 is not null || !string.IsNullOrEmpty(Comment);
        if (hasData)
        {
            var confirmed = await Shell.Current.DisplayAlert("Annuler", "Des données ont été saisies. Quitter quand même ?", "Oui", "Non");
            if (!confirmed) return;
        }
        await Shell.Current.GoToAsync("..");
    }
}
