using DropFlow.Shared.Drivers;
using DropFlow.WebApp.Components.Shared;
using DropFlow.WebApp.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DropFlow.WebApp.Components.Pages.Settings.Drivers;

public partial class Drivers : ComponentBase
{
    [Inject] private IDriverService DriverService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    #region Private Fields

    private List<DriverDto> _drivers = [];
    private List<DriverDto> _filteredDrivers = [];
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
        await LoadDrivers();
    }

    #endregion

    #region Breadcrumbs

    private void InitializeBreadcrumbs()
    {
        _breadcrumbs =
        [
            new BreadcrumbItem("Paramètres", href: "/settings"),
            new BreadcrumbItem("Livreurs", href: "/settings/livreurs", disabled: true)
        ];
    }

    #endregion

    #region Load Data

    /// <summary>
    /// Charge les livreurs depuis l'API
    /// </summary>
    private async Task LoadDrivers()
    {
        _loading = true;
        try
        {
            _drivers = await DriverService.GetAllAsync(forceRefresh: true);
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

    /// <summary>
    /// Applique les filtres de recherche et statut
    /// </summary>
    private void ApplyFilters()
    {
        _filteredDrivers = _drivers;

        // Filtre par recherche (FirstName, LastName, Email, LicenseNumber)
        if (!string.IsNullOrWhiteSpace(_searchTerm))
        {
            var search = _searchTerm.ToLowerInvariant();
            _filteredDrivers = _filteredDrivers.Where(d =>
                (d.FirstName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.LastName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.LicenseNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        // Filtre par statut
        if (_filterIsActive.HasValue)
        {
            _filteredDrivers = _filteredDrivers.Where(d => d.IsActive == _filterIsActive.Value).ToList();
        }
    }

    #endregion

    #region Dialog Management

    /// <summary>
    /// Ouvre le dialog de création
    /// </summary>
    private async Task OpenCreateDialog()
    {
        var dialog = await DialogService.ShowAsync<DriverDialog>("Nouveau livreur", new DialogOptions
        {
            Position = DialogPosition.Center,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        });

        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            await LoadDrivers();
        }
    }

    /// <summary>
    /// Ouvre le dialog d'édition
    /// </summary>
    private async Task OpenEditDialog(DriverDto driver)
    {
        var parameters = new DialogParameters<DriverDialog>
        {
            { x => x.DriverId, driver.Id },
            { x => x.ExistingDriver, driver }
        };

        var dialog = await DialogService.ShowAsync<DriverDialog>("Modifier livreur", parameters, new DialogOptions
        {
            Position = DialogPosition.Center,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        });

        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            await LoadDrivers();
        }
    }

    #endregion

    #region CRUD Operations

    private async Task ToggleDriverStatus(DriverDto driver)
    {
        var newStatus = !driver.IsActive;
        var action = newStatus ? "activer" : "désactiver";

        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, $"Voulez-vous {action} le livreur '{driver.FullName}' ?" },
            { x => x.ButtonText, newStatus ? "Activer" : "Désactiver" },
            { x => x.Color, newStatus ? Color.Success : Color.Warning }
        };

        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirmation", parameters);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            try
            {
                var updateDto = new UpdateDriverDto
                {
                    LicenseNumber = driver.LicenseNumber,
                    LicenseExpiryDate = driver.LicenseExpiryDate,
                    IsActive = newStatus
                };

                var updateResult = await DriverService.UpdateAsync(driver.Id, updateDto);

                if (updateResult.Succeeded)
                {
                    Snackbar.Add($"Livreur {(newStatus ? "activé" : "désactivé")} avec succès", Severity.Success);
                    await LoadDrivers();
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