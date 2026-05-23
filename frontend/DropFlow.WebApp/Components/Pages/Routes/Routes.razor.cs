using DropFlow.Shared.Enums;
using DropFlow.Shared.Routes;
using DropFlow.WebApp.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DropFlow.WebApp.Components.Pages.Routes;

public partial class Routes
{
    [Inject] private IRouteService RouteService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILogger<Routes> Logger { get; set; } = default!;

    private List<RouteViewDto> _routes = [];
    private bool _loading = true;

    private string? _searchTerm;
    private DateTime? _filterDate;
    private RouteStatus? _filterStatus;

    private int _currentPage = 1;
    private readonly int _pageSize = 20;

    private bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(_searchTerm) || _filterDate.HasValue || _filterStatus.HasValue;

    protected override async Task OnInitializedAsync()
    {
        await LoadRoutes();
    }

    private async Task LoadRoutes()
    {
        _loading = true;

        try
        {
            var filter = new RouteFilterDto
            {
                SearchTerm = _searchTerm?.Trim(),
                Date = _filterDate,
                Status = _filterStatus,
                Page = _currentPage,
                PageSize = _pageSize
            };

            var result = await RouteService.GetRoutesAsync(filter);
            _routes = result.Items.ToList();
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"❌ Error loading routes: {ex.Message}");
            Snackbar.Add("Erreur lors du chargement des tournées", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OnSearchChangedAsync(string? value)
    {
        _searchTerm = value;
        _currentPage = 1;
        await LoadRoutes();
    }

    private async Task OnDateChangedAsync(DateTime? date)
    {
        _filterDate = date;
        _currentPage = 1;
        await LoadRoutes();
    }

    private async Task OnStatusChangedAsync(RouteStatus? status)
    {
        _filterStatus = status;
        _currentPage = 1;
        await LoadRoutes();
    }

    private async Task ClearSearchAsync()
    {
        _searchTerm = null;
        _currentPage = 1;
        await LoadRoutes();
    }

    private async Task ClearDateAsync()
    {
        _filterDate = null;
        _currentPage = 1;
        await LoadRoutes();
    }

    private async Task ClearStatusAsync()
    {
        _filterStatus = null;
        _currentPage = 1;
        await LoadRoutes();
    }

    private async Task ResetFilters()
    {
        _searchTerm = null;
        _filterDate = null;
        _filterStatus = null;
        _currentPage = 1;
        await LoadRoutes();
    }

    private void ViewDetails(int id) => NavigationManager.NavigateTo($"/tournees/{id}");

    private void Edit(int id) => NavigationManager.NavigateTo($"/tournees/edit/{id}");

    private async Task Delete(int id, string reference)
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Confirmation",
            $"Voulez-vous vraiment supprimer la feuille de route {reference} ?",
            yesText: "Supprimer",
            cancelText: "Annuler");

        if (confirmed == true)
        {
            var result = await RouteService.DeleteRouteAsync(id);

            if (result.Succeeded)
            {
                Snackbar.Add($"Feuille de route {reference} supprimée", Severity.Success);
                await LoadRoutes();
            }
            else
            {
                Snackbar.Add(result.Errors.FirstOrDefault() ?? "Erreur lors de la suppression", Severity.Error);
            }
        }
    }

    private string GetStatusDisplay(RouteStatus status) => status.Humanize();

    private static Color GetRouteStatusColor(RouteStatus status) => status switch
    {
        RouteStatus.Draft      => Color.Default,
        RouteStatus.Confirmed  => Color.Info,
        RouteStatus.InProgress => Color.Warning,
        RouteStatus.Completed  => Color.Success,
        RouteStatus.Cancelled  => Color.Error,
        _                      => Color.Default
    };

    private static string GetRowStyle(RouteViewDto route)
    {
        var color = route.Status switch
        {
            RouteStatus.Draft      => "#9CA3AF",
            RouteStatus.Confirmed  => "#3B82F6",
            RouteStatus.InProgress => "#F59E0B",
            RouteStatus.Completed  => "#10B981",
            RouteStatus.Cancelled  => "#EF4444",
            _                      => "#E5E7EB"
        };
        return $"border-left: 3px solid {color};";
    }

    private static string FormatDuration(int minutes)
    {
        var hours = minutes / 60;
        var mins = minutes % 60;
        return hours > 0 ? $"{hours}h{mins:D2}" : $"{mins}min";
    }
}
