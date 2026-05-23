using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DropFlow.Mobile.Models;
using DropFlow.Mobile.Services;

namespace DropFlow.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly AuthStorageService _auth;

    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private bool _isPasswordVisible;
    [ObservableProperty] private bool _rememberMe = true;
    [ObservableProperty] private int _selectedTenantId = 1;

    public LoginViewModel(ApiService api, AuthStorageService auth, ConnectivityService connectivity)
        : base(connectivity)
    {
        _api = api;
        _auth = auth;
    }

    public async Task CheckExistingSessionAsync()
    {
        if (await _auth.HasValidTokenAsync())
            await Shell.Current.GoToAsync("//route");
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            HasError = true;
            ErrorMessage = "Veuillez remplir tous les champs";
            return;
        }

        await ExecuteAsync(async () =>
        {
            var result = await _api.LoginAsync(new LoginRequest
            {
                Email = Email,
                Password = Password,
                TenantId = SelectedTenantId
            });

            if (!result.Success || result.Token is null || result.User is null)
            {
                HasError = true;
                ErrorMessage = result.Message ?? "Identifiants incorrects";
                return;
            }

            await _auth.SaveAsync(result.Token, result.User, result.RefreshToken, RememberMe);
            await Shell.Current.GoToAsync("//route");
        });
    }

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private void ToggleRememberMe() => RememberMe = !RememberMe;
}
