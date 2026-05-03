using DropFlow.Shared.Vehicles;
using DropFlow.WebApp.Components.Shared;
using DropFlow.WebApp.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DropFlow.WebApp.Components.Pages.Settings.Vehicles;

public partial class Vehicles : ComponentBase
{
    [Inject] private IVehicleService VehicleService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    #region Private Fields

    private List<VehicleDto> _vehicles = [];
    private List<VehicleDto> _filteredVehicles = [];
    private bool _loading = true;

    // Filtres
    private string _searchTerm = string.Empty;
    private bool? _filterIsActive;

    private List<BreadcrumbItem> _breadcrumbs = [];

    #endregion

    #region Lifecycle

    protected override async Task OnInitializedAsync()
    {
        InitializeBreadcrumbs();
        await LoadVehicles();
    }

    #endregion

    #region Breadcrumbs

    private void InitializeBreadcrumbs()
    {
        _breadcrumbs =
        [
            new BreadcrumbItem("Paramètres", href: "/settings"),
            new BreadcrumbItem("Véhicules", href: "/settings/vehicules", disabled: true)
        ];
    }

    #endregion

    #region Load Data

    /// <summary>
    /// Charge les véhicules depuis l'API
    /// </summary>
    private async Task LoadVehicles()
    {
        _loading = true;
        try
        {
            _vehicles = await VehicleService.GetAllVehiclesAsync(forceRefresh: true);
            ApplyFilters();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur lors du chargement : {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    #endregion

    #region Filters

    /// <summary>
    /// Appelé quand le terme de recherche change
    /// </summary>
    private void OnSearchChanged()
    {
        ApplyFilters();
    }

    private void OnSearchChanged(bool? isActive)
    {
        _filterIsActive = isActive;
        ApplyFilters();
    }
    
    /// <summary>
    /// Applique les filtres de recherche et statut
    /// </summary>
    private void ApplyFilters()
    {
        _filteredVehicles = _vehicles;

        // Filtre par recherche (Brand, Model, PlateNumber)
        if (!string.IsNullOrWhiteSpace(_searchTerm))
        {
            var search = _searchTerm.ToLowerInvariant();
            _filteredVehicles = _filteredVehicles.Where(v =>
                (v.Brand?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.Model?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (v.PlateNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        // Filtre par statut
        if (_filterIsActive.HasValue)
        {
            _filteredVehicles = _filteredVehicles.Where(v => v.IsActive == _filterIsActive.Value).ToList();
        }
    }

    #endregion

    #region Dialog Management

    /// <summary>
    /// Ouvre le dialog de création
    /// </summary>
    private async Task OpenCreateDialog()
    {
        var dialog = await DialogService.ShowAsync<VehicleDialog>("Nouveau véhicule", new DialogOptions
        {
            Position = DialogPosition.Center,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        });

        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            await LoadVehicles();
        }
    }

    /// <summary>
    /// Ouvre le dialog d'édition
    /// </summary>
    private async Task OpenEditDialog(VehicleDto vehicle)
    {
        var parameters = new DialogParameters<VehicleDialog>
        {
            { x => x.VehicleId, vehicle.Id },
            { x => x.ExistingVehicle, vehicle }
        };

        var dialog = await DialogService.ShowAsync<VehicleDialog>("Modifier véhicule", parameters, new DialogOptions
        {
            Position = DialogPosition.Center,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        });

        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            await LoadVehicles();
        }
    }

    #endregion

    #region CRUD Operations

    /// <summary>
    /// Supprime un véhicule après confirmation
    /// </summary>
    private async Task DeleteVehicle(VehicleDto vehicle)
    {
        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, $"Êtes-vous sûr de vouloir supprimer le véhicule '{vehicle.DisplayName}' ?" },
            { x => x.ButtonText, "Supprimer" },
            { x => x.Color, Color.Error }
        };

        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirmation", parameters);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            try
            {
                var deleteResult = await VehicleService.DeleteVehicleAsync(vehicle.Id);

                if (deleteResult.Succeeded)
                {
                    Snackbar.Add("Véhicule supprimé avec succès", Severity.Success);
                    await LoadVehicles();
                }
                else
                {
                    var errorMessage = deleteResult.Errors?.FirstOrDefault() ?? "Erreur lors de la suppression";
                    Snackbar.Add(errorMessage, Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Erreur : {ex.Message}", Severity.Error);
            }
        }
    }

    /// <summary>
    /// Toggle le statut IsActive d'un véhicule
    /// </summary>
    private async Task ToggleVehicleStatus(VehicleDto vehicle)
    {
        var newStatus = !vehicle.IsActive;
        var action = newStatus ? "activer" : "désactiver";

        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, $"Voulez-vous {action} le véhicule '{vehicle.DisplayName}' ?" },
            { x => x.ButtonText, newStatus ? "Activer" : "Désactiver" },
            { x => x.Color, newStatus ? Color.Success : Color.Warning }
        };

        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirmation", parameters);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            try
            {
                var updateDto = new UpdateVehicleDto
                {
                    Brand = vehicle.Brand,
                    Model = vehicle.Model,
                    PlateNumber = vehicle.PlateNumber,
                    MaxDeliveries = vehicle.MaxDeliveries,
                    MaxVolume = vehicle.MaxVolume,
                    IsActive = newStatus
                };

                var updateResult = await VehicleService.UpdateVehicleAsync(vehicle.Id, updateDto);

                if (updateResult.Succeeded)
                {
                    Snackbar.Add($"Véhicule {(newStatus ? "activé" : "désactivé")} avec succès", Severity.Success);
                    await LoadVehicles();
                }
                else
                {
                    var errorMessage = updateResult.Errors?.FirstOrDefault() ?? "Erreur lors du changement de statut";
                    Snackbar.Add(errorMessage, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Erreur lors du changement de statut : {ex.Message}", Severity.Error);
            }
        }
    }

    #endregion
}