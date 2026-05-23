using CommunityToolkit.Mvvm.ComponentModel;
using DropFlow.Mobile.Services;

namespace DropFlow.Mobile.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    protected readonly ConnectivityService ConnectivityService;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isOffline;

    protected BaseViewModel(ConnectivityService connectivityService)
    {
        ConnectivityService = connectivityService;
        IsOffline = !connectivityService.IsConnected;
        connectivityService.ConnectivityChanged += OnConnectivityChanged;
    }

    private void OnConnectivityChanged(object? sender, bool isConnected)
    {
        MainThread.BeginInvokeOnMainThread(() => IsOffline = !isConnected);
    }

    protected async Task ExecuteAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            await action();
        }
        catch (NoConnectivityException)
        {
            HasError = true;
            ErrorMessage = "📡 Pas de connexion Internet";
        }
        catch (SessionExpiredException)
        {
            await Shell.Current.GoToAsync("//login");
        }
        catch (HttpRequestException)
        {
            HasError = true;
            ErrorMessage = "🔌 Impossible de joindre le serveur";
        }
        catch (TaskCanceledException)
        {
            HasError = true;
            ErrorMessage = "⏱️ Le serveur ne répond pas";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Une erreur est survenue : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
