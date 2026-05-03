using DropFlow.Shared.Stores;
using DropFlow.WebApp.Components.Shared;
using DropFlow.WebApp.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DropFlow.WebApp.Components.Pages.Settings.Stores;

public partial class Stores : ComponentBase
{
    [Inject] private IStoreService StoreService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    #region Private Fields

    private List<StoreDto> _stores = [];
    private List<StoreDto> _filteredStores = [];
    private bool _loading;

    // Filtres
    private string _searchTerm = string.Empty;
    private bool? _filterIsActive;

    #endregion

    #region Lifecycle

    protected override async Task OnInitializedAsync()
    {
        await LoadStores();
    }

    #endregion

    #region Load Data

    /// <summary>
    /// Charge les enseignes depuis l'API
    /// </summary>
    private async Task LoadStores()
    {
        _loading = true;
        StateHasChanged();

        try
        {
            _stores = await StoreService.GetAllStoresAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur lors du chargement : {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
            StateHasChanged();
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
        StateHasChanged();
    }

    /// <summary>
    /// Applique les filtres de recherche et statut
    /// </summary>
    private void ApplyFilters()
    {
        _filteredStores = _stores;

        // Filtre par recherche (Nom, Ville, ContactName)
        if (!string.IsNullOrWhiteSpace(_searchTerm))
        {
            var search = _searchTerm.ToLowerInvariant();
            _filteredStores = _filteredStores.Where(s =>
                (s.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.City?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.ContactName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        // Filtre par statut
        if (_filterIsActive.HasValue)
        {
            _filteredStores = _filteredStores.Where(s => s.IsActive == _filterIsActive.Value).ToList();
        }
    }

    #endregion

    #region Dialog Management

    /// <summary>
    /// Ouvre le dialog de création
    /// </summary>
    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters
        {
            ["IsEditMode"] = false
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseOnEscapeKey = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<StoreDialog>("Nouvelle Enseigne", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: CreateStoreDto createDto })
        {
            await CreateStore(createDto);
        }
    }

    /// <summary>
    /// Ouvre le dialog d'édition
    /// </summary>
    private async Task OpenEditDialog(StoreDto store)
    {
        var parameters = new DialogParameters
        {
            ["IsEditMode"] = true,
            ["StoreId"] = store.Id,
            ["ExistingStore"] = store
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseOnEscapeKey = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<StoreDialog>("Modifier l'Enseigne", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: UpdateStoreDto updateDto })
        {
            await UpdateStore(store.Id, updateDto);
        }
    }

    #endregion

    #region CRUD Operations

    /// <summary>
    /// Crée une nouvelle enseigne
    /// </summary>
    private async Task CreateStore(CreateStoreDto dto)
    {
        try
        {
            var result = await StoreService.CreateStoreAsync(dto);

            if (result.Succeeded)
            {
                Snackbar.Add("Enseigne créée avec succès", Severity.Success);
                await LoadStores();
            }
            else
            {
                Snackbar.Add($"Erreur : {string.Join((string?)", ", (IEnumerable<string?>)result.Errors)}",
                    Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur lors de la création : {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Met à jour une enseigne existante
    /// </summary>
    private async Task UpdateStore(int id, UpdateStoreDto dto)
    {
        try
        {
            var result = await StoreService.UpdateStoreAsync(id, dto);

            if (result.Succeeded)
            {
                Snackbar.Add("Enseigne mise à jour avec succès", Severity.Success);
                await LoadStores();
            }
            else
            {
                Snackbar.Add($"Erreur : {string.Join((string?)", ", (IEnumerable<string?>)result.Errors)}",
                    Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur lors de la mise à jour : {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Supprime une enseigne après confirmation
    /// </summary>
    private async Task DeleteStore(StoreDto store)
    {
        var parameters = new DialogParameters
        {
            ["ContentText"] = $"Êtes-vous sûr de vouloir supprimer l'enseigne '{store.Name}' ?",
            ["ButtonText"] = "Supprimer",
            ["Color"] = Color.Error
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small
        };

        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirmation", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            try
            {
                var deleteResult = await StoreService.DeleteStoreAsync(store.Id);

                if (deleteResult.Succeeded)
                {
                    Snackbar.Add("Enseigne supprimée avec succès", Severity.Success);
                    await LoadStores();
                }
                else
                {
                    Snackbar.Add($"Erreur : {string.Join((string?)", ", deleteResult.Errors)}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Erreur lors de la suppression : {ex.Message}", Severity.Error);
            }
        }
    }

    /// <summary>
    /// Toggle le statut IsActive d'une enseigne
    /// </summary>
    private async Task ToggleStoreStatus(StoreDto store)
    {
        var newStatus = !store.IsActive;
        var action = newStatus ? "activer" : "désactiver";

        var parameters = new DialogParameters
        {
            ["ContentText"] = $"Voulez-vous {action} l'enseigne '{store.Name}' ?",
            ["ButtonText"] = newStatus ? "Activer" : "Désactiver",
            ["Color"] = newStatus ? Color.Success : Color.Warning
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small
        };

        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirmation", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            try
            {
                var updateDto = new UpdateStoreDto
                {
                    Name = store.Name,
                    Address = store.Address,
                    ZipCode = store.ZipCode,
                    City = store.City,
                    ContactName = store.ContactName,
                    Phone = store.Phone,
                    Email = store.Email,
                    Notes = store.Notes,
                    IsActive = newStatus
                };

                var updateResult = await StoreService.UpdateStoreAsync(store.Id, updateDto);

                if (updateResult.Succeeded)
                {
                    Snackbar.Add($"Enseigne {(newStatus ? "activée" : "désactivée")} avec succès", Severity.Success);
                    await LoadStores();
                }
                else
                {
                    Snackbar.Add($"Erreur : {string.Join((string?)", ", updateResult.Errors)}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Erreur lors du changement de statut : {ex.Message}", Severity.Error);
            }
        }
    }

    #endregion

    private Task IsActivesFilterChanged(bool? isActive)
    {
        _filterIsActive = isActive;
        ApplyFilters();
        return Task.CompletedTask;
    }
}