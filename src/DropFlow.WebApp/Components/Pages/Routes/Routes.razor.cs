using DropFlow.Domain.Enums;
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
    
    private List<RouteViewDto> _routes = [];
    private bool _loading = true;

    // Filtres
    private DateTime? _filterDate;
    private RouteStatus? _filterStatus;

    // Pagination
    private int _currentPage = 1;
    private readonly int _pageSize = 20;

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
            Console.WriteLine($"❌ Error loading routes: {ex.Message}");
            Snackbar.Add("Erreur lors du chargement des tournées", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ApplyFilters()
    {
        _currentPage = 1;
        await LoadRoutes();
    }

    private async Task ResetFilters()
    {
        _filterDate = null;
        _filterStatus = null;
        _currentPage = 1;
        await LoadRoutes();
    }

    private void ViewDetails(int id)
    {
        NavigationManager.NavigateTo($"/tournees/{id}");
    }

    private void Edit(int id)
    {
        NavigationManager.NavigateTo($"/tournees/edit/{id}");
    }

    // ✅ SIMPLIFIÉ : Gestion par le composant RouteStatusActions
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

    // ✅ Méthodes Start/Complete retirées (gérées par RouteStatusActions)
    // private async Task Start(int id, string reference) { ... }
    // private async Task Complete(int id, string reference) { ... }

    private string GetStatusDisplay(RouteStatus status)
    {
        return status.Humanize();
    }
    
    private static string FormatDuration(int minutes)
    {
        var hours = minutes / 60;
        var mins = minutes % 60;

        if (hours > 0)
            return $"{hours}h{mins:D2}";
        return $"{mins}min";
    }
}