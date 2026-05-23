using DropFlow.Shared.Enums;
using DropFlow.Shared.Common;
using DropFlow.Shared.Deliveries;
using DropFlow.Shared.Stores;
using DropFlow.WebApp.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DropFlow.WebApp.Components.Pages.Deliveries;

public partial class Deliveries
{
    // ════════════════════════════════════════════════════════════════
    // SERVICES INJECTÉS
    // ════════════════════════════════════════════════════════════════

    [Inject] private IDeliveryService DeliveryService { get; set; } = default!;
    [Inject] private IStoreService StoreService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILogger<Deliveries> Logger { get; set; } = default!;

    // ════════════════════════════════════════════════════════════════
    // QUERY PARAMETERS
    // ════════════════════════════════════════════════════════════════

    [Parameter]
    [SupplyParameterFromQuery(Name = "status")]
    public string? StatusQuery { get; set; }

    // ════════════════════════════════════════════════════════════════
    // STATE
    // ════════════════════════════════════════════════════════════════

    private bool _isLoading = true;
    private List<DeliveryViewDto> _deliveries = new();
    private PagedResult<DeliveryViewDto>? _pagedResult;
    private HashSet<DeliveryViewDto> _selectedDeliveries = new();
    private HashSet<DeliveryViewDto> _selectedTableItems = new();
    private List<StoreDto> _stores = [];
    private ViewMode _viewMode = ViewMode.List;
    private int _totalCount;

    // Table reference pour ServerData
    private MudTable<DeliveryViewDto>? _tableRef;

    // Filtres
    private DeliveryFilterDto _filters = new()
    {
        Page = 1,
        PageSize = 20,
        DateFrom = null, // Par défaut : à partir d'aujourd'hui
        SortBy = "SequentialNumber",
        SortDescending = true
    };

    private string? _searchTerm;
    private int? _selectedStoreId;
    private DeliveryStatus? _selectedStatus;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;

    // ════════════════════════════════════════════════════════════════
    // ENUMS
    // ════════════════════════════════════════════════════════════════

    private enum ViewMode
    {
        Cards,
        List
    }

    // ════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ════════════════════════════════════════════════════════════════

    protected override async Task OnInitializedAsync()
    {
        await LoadStoresAsync();

        // Appliquer le filtre status issu du query string (?status=ToBePlanned, etc.)
        if (!string.IsNullOrEmpty(StatusQuery) &&
            Enum.TryParse<DeliveryStatus>(StatusQuery, ignoreCase: true, out var status))
        {
            _selectedStatus = status;
            _filters.Statuses = [status];
        }

        // En mode Cards, charger les données directement
        if (_viewMode == ViewMode.Cards)
        {
            await LoadDeliveriesAsync();
        }
        // En mode List, MudTable chargera automatiquement via ServerData
    }

    // ════════════════════════════════════════════════════════════════
    // CHARGEMENT DES DONNÉES
    // ════════════════════════════════════════════════════════════════

    private async Task LoadDeliveriesAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            // Appliquer les filtres UI aux filtres DTO
            _filters.GlobalSearch = _searchTerm;
            _filters.StoreId = _selectedStoreId;
           
            if (_selectedStatus.HasValue)
            {
                _filters.Statuses = [_selectedStatus.Value];
            }
            else
            {
                // Sinon, garder les statuts par défaut (non livrés/annulés)
                _filters.Statuses ??=
                [
                    DeliveryStatus.ToBePlanned,
                    DeliveryStatus.Confirmed,
                    DeliveryStatus.InProgress
                ];
            }
            
            _filters.DateFrom = _dateFrom;
            _filters.DateTo = _dateTo;

            _pagedResult = await DeliveryService.GetDeliveriesAsync(_filters);

            if (_pagedResult != null)
            {
                _deliveries = _pagedResult.Items.ToList();
                _totalCount = _pagedResult.TotalCount;
            }
            else
            {
                _deliveries = new List<DeliveryViewDto>();
                _totalCount = 0;
                Snackbar.Add("Erreur lors du chargement des livraisons", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur : {ex.Message}", Severity.Error);
            _deliveries = new List<DeliveryViewDto>();
            _totalCount = 0;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// ServerData pour MudTable - appelé automatiquement par le composant
    /// </summary>
    private async Task<TableData<DeliveryViewDto>> ServerReload(TableState state, CancellationToken token)
    {
        _isLoading = true;

        try
        {
            // Appliquer les filtres
            _filters.GlobalSearch = _searchTerm;
            _filters.StoreId = _selectedStoreId;
            
            if (_selectedStatus.HasValue)
            {
                _filters.Statuses = [_selectedStatus.Value];
            }
            else
            {
                _filters.Statuses ??=
                [
                    DeliveryStatus.ToBePlanned,
                    DeliveryStatus.Confirmed,
                    DeliveryStatus.InProgress
                ];
            }
            
            _filters.DateFrom = _dateFrom;
            _filters.DateTo = _dateTo;
            
            // Pagination depuis TableState
            _filters.Page = state.Page + 1; // TableState est 0-indexed
            _filters.PageSize = state.PageSize;
            
            // Tri depuis TableState
            if (!string.IsNullOrEmpty(state.SortLabel))
            {
                _filters.SortBy = state.SortLabel;
                _filters.SortDescending = state.SortDirection == SortDirection.Descending;
            }

            var result = await DeliveryService.GetDeliveriesAsync(_filters);

            if (result != null)
            {
                _deliveries = result.Items.ToList();
                _totalCount = result.TotalCount;
                
                // Synchroniser la sélection avec les nouveaux items
                SyncTableSelection();
                
                return new TableData<DeliveryViewDto>
                {
                    TotalItems = result.TotalCount,
                    Items = result.Items
                };
            }
            
            return new TableData<DeliveryViewDto> 
            { 
                TotalItems = 0, 
                Items = Array.Empty<DeliveryViewDto>() 
            };
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur : {ex.Message}", Severity.Error);
            return new TableData<DeliveryViewDto> 
            { 
                TotalItems = 0, 
                Items = Array.Empty<DeliveryViewDto>() 
            };
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadStoresAsync()
    {
        try
        {
            _stores = await StoreService.GetAllStoresAsync();
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"Error loading stores: {ex.Message}");
            _stores = new List<StoreDto>();
        }
    }

    // ════════════════════════════════════════════════════════════════
    // FILTRES
    // ════════════════════════════════════════════════════════════════

    private async Task OnSearchChanged()
    {
        _filters.Page = 1;
        await ReloadDeliveriesAsync();
    }

    private async Task OnStoreChangedAsync(int? storeId)
    {
        _selectedStoreId = storeId;
        _filters.Page = 1;
        await ReloadDeliveriesAsync();
    }

    private async Task OnStatusChangedAsync(DeliveryStatus? status)
    {
        _selectedStatus = status;
        _filters.Page = 1;
        await ReloadDeliveriesAsync();
    }

    private async Task OnDateFromChangedAsync(DateTime? date)
    {
        _dateFrom = date;
        _filters.Page = 1;
        await ReloadDeliveriesAsync();
    }

    private async Task OnDateToChangedAsync(DateTime? date)
    {
        _dateTo = date;
        _filters.Page = 1;
        await ReloadDeliveriesAsync();
    }

    private async Task ClearDatesAsync()
    {
        _dateFrom = null;
        _dateTo = null;
        _filters.Page = 1;
        await ReloadDeliveriesAsync();
    }

    private async Task ClearSearchAsync()
    {
        _searchTerm = null;
        _filters.Page = 1;
        await ReloadDeliveriesAsync();
    }

    private async Task ResetFiltersAsync()
    {
        _searchTerm = null;
        _selectedStoreId = null;
        _selectedStatus = null;
        _dateFrom = null;
        _dateTo = null;

        _filters = new DeliveryFilterDto
        {
            Page = 1,
            PageSize = _filters.PageSize,
            Statuses =
            [
                DeliveryStatus.ToBePlanned,
                DeliveryStatus.Confirmed,
                DeliveryStatus.InProgress
            ],
            SortBy = "SequentialNumber",
            SortDescending = true
        };

        await ReloadDeliveriesAsync();
    }

    // ════════════════════════════════════════════════════════════════
    // PAGINATION (pour mode Cards)
    // ════════════════════════════════════════════════════════════════

    private async Task OnPageChanged(int page)
    {
        _filters.Page = page;
        await LoadDeliveriesAsync();
    }

    private async Task OnPageSizeChanged(int pageSize)
    {
        _filters.PageSize = pageSize;
        _filters.Page = 1;
        await LoadDeliveriesAsync();
    }

    // ════════════════════════════════════════════════════════════════
    // SÉLECTION - MODE CARDS
    // ════════════════════════════════════════════════════════════════

    private bool IsSelected(DeliveryViewDto delivery)
    {
        return _selectedDeliveries.Contains(delivery);
    }

    private void ToggleSelection(DeliveryViewDto delivery)
    {
        if (!_selectedDeliveries.Add(delivery))
        {
            _selectedDeliveries.Remove(delivery);
        }

        StateHasChanged();
    }

    private void ClearSelection()
    {
        _selectedDeliveries.Clear();
        _selectedTableItems.Clear();
        StateHasChanged();
    }

    // ════════════════════════════════════════════════════════════════
    // SÉLECTION - MODE TABLE
    // ════════════════════════════════════════════════════════════════

    private void OnSelectedTableItemsChanged(HashSet<DeliveryViewDto> items)
    {
        // Synchroniser avec _selectedDeliveries
        _selectedTableItems = items;
        _selectedDeliveries = [..items];
        StateHasChanged();
    }

    private void SyncTableSelection()
    {
        // Synchroniser la sélection après rechargement des données
        var selectedIds = _selectedDeliveries.Select(d => d.Id).ToHashSet();
        _selectedTableItems = _deliveries.Where(d => selectedIds.Contains(d.Id)).ToHashSet();
    }

    // ════════════════════════════════════════════════════════════════
    // ACTIONS CRUD - SIMPLE
    // ════════════════════════════════════════════════════════════════

    private void OpenCreateDialog()
    {
        NavigationManager.NavigateTo("/livraisons/nouvelle", true);
    }

    private void ViewDetailsAsync(int id)
    {
        NavigationManager.NavigateTo($"/livraisons/{id}", true);
    }

    private void EditDeliveryAsync(int id)
    {
        NavigationManager.NavigateTo($"/livraisons/edit/{id}", true);
    }

    private async Task DeleteDeliveryAsync(int id)
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Supprimer la livraison",
            "Êtes-vous sûr de vouloir supprimer cette livraison ? Cette action est irréversible.",
            yesText: "Supprimer",
            cancelText: "Annuler");

        if (confirmed == true)
        {
            var result = await DeliveryService.DeleteDeliveryAsync(id);

            if (result.Succeeded)
            {
                Snackbar.Add("Livraison supprimée avec succès", Severity.Success);
                _selectedDeliveries.Clear();
                _selectedTableItems.Clear();
                
                await ReloadDeliveriesAsync();
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    Snackbar.Add(error, Severity.Error);
                }
            }
        }
    }

    private async Task GeneratePdfAsync(int id)
    {
        // TODO: Générer PDF
        Snackbar.Add($"Génération PDF livraison #{id} à implémenter", Severity.Info);
    }

    // ════════════════════════════════════════════════════════════════
    // ACTIONS GROUPÉES
    // ════════════════════════════════════════════════════════════════

    private async Task BulkChangeStatusAsync(DeliveryStatus newStatus)
    {
        if (!_selectedDeliveries.Any())
        {
            Snackbar.Add("Veuillez sélectionner au moins une livraison", Severity.Warning);
            return;
        }

        var confirmed = await DialogService.ShowMessageBox(
            "Changer le statut",
            $"Voulez-vous changer le statut de {_selectedDeliveries.Count} livraison(s) à '{GetStatusLabel(newStatus)}' ?",
            yesText: "Confirmer",
            cancelText: "Annuler");

        if (confirmed == true)
        {
            var ids = _selectedDeliveries.Select(d => d.Id).ToList();
            var result = await DeliveryService.BulkUpdateStatusAsync(ids, newStatus);

            if (result.Succeeded)
            {
                Snackbar.Add(result.Message ?? $"{ids.Count} livraison(s) mise(s) à jour", Severity.Success);
                _selectedDeliveries.Clear();
                _selectedTableItems.Clear();

                await ReloadDeliveriesAsync();
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    Snackbar.Add(error, Severity.Error);
                }
            }
        }
    }

    private async Task ExportSelectedAsync()
    {
        if (!_selectedDeliveries.Any())
        {
            Snackbar.Add("Veuillez sélectionner au moins une livraison", Severity.Warning);
            return;
        }

        // TODO: Implémenter export Excel
        Snackbar.Add($"Export de {_selectedDeliveries.Count} livraison(s) à implémenter", Severity.Info);
    }

    private async Task DeleteSelectedAsync()
    {
        if (!_selectedDeliveries.Any())
        {
            Snackbar.Add("Veuillez sélectionner au moins une livraison", Severity.Warning);
            return;
        }

        var confirmed = await DialogService.ShowMessageBox(
            "Supprimer les livraisons",
            $"Êtes-vous sûr de vouloir supprimer {_selectedDeliveries.Count} livraison(s) ? Cette action est irréversible.",
            yesText: "Supprimer",
            cancelText: "Annuler");

        if (confirmed == true)
        {
            var successCount = 0;
            var errorCount = 0;

            foreach (var delivery in _selectedDeliveries.ToList())
            {
                var result = await DeliveryService.DeleteDeliveryAsync(delivery.Id);

                if (result.Succeeded)
                {
                    successCount++;
                }
                else
                {
                    errorCount++;
                }
            }

            if (successCount > 0)
            {
                Snackbar.Add($"{successCount} livraison(s) supprimée(s)", Severity.Success);
            }

            if (errorCount > 0)
            {
                Snackbar.Add($"{errorCount} erreur(s) lors de la suppression", Severity.Error);
            }

            _selectedDeliveries.Clear();
            _selectedTableItems.Clear();

            await ReloadDeliveriesAsync();
        }
    }

    private async Task ExportAllAsync()
    {
        // TODO: Implémenter export Excel complet
        Snackbar.Add("Export complet à implémenter", Severity.Info);
    }

    // ════════════════════════════════════════════════════════════════
    // HELPERS - STYLE & AFFICHAGE
    // ════════════════════════════════════════════════════════════════

    private static string GetRowStyle(DeliveryViewDto delivery)
    {
        var color = delivery.Status switch
        {
            DeliveryStatus.ToBePlanned => "#F59E0B",
            DeliveryStatus.Confirmed   => "#3B82F6",
            DeliveryStatus.InProgress  => "#8B5CF6",
            DeliveryStatus.Delivered   => "#10B981",
            DeliveryStatus.Canceled    => "#9CA3AF",
            _ => "#E5E7EB"
        };
        return $"border-left: 3px solid {color};";
    }

    private string GetCardStyle(DeliveryViewDto delivery)
    {
        var borderColor = delivery.Status switch
        {
            DeliveryStatus.ToBePlanned => "#F59E0B",
            DeliveryStatus.Confirmed => "#3B82F6",
            DeliveryStatus.InProgress => "#8B5CF6",
            DeliveryStatus.Delivered => "#10B981",
            DeliveryStatus.Canceled => "#6B7280",
            _ => "#E5E7EB"
        };

        var isSelected = IsSelected(delivery);
        var background = isSelected ? "#EFF6FF" : "#FFFFFF";

        return $"border-left: 4px solid {borderColor}; " +
               $"background: {background}; " +
               $"padding: 16px; ";
    }

    private static string GetStatusBadgeStyle(DeliveryStatus status)
    {
        return status switch
        {
            DeliveryStatus.ToBePlanned => "background: #FEF3C7; color: #D97706; border-radius: 6px;",
            DeliveryStatus.Confirmed => "background: #DBEAFE; color: #1D4ED8; border-radius: 6px;",
            DeliveryStatus.InProgress => "background: #EDE9FE; color: #7C3AED;  border-radius: 6px;",
            DeliveryStatus.Delivered => "background: #D1FAE5; color: #059669;  border-radius: 6px;",
            DeliveryStatus.Canceled => "background: #F3F4F6; color: #4B5563; border-radius: 6px;",
            _ => "background: #F3F4F6; color: #6B7280;  border-radius: 6px;"
        };
    }

    private static string GetStatusEmoji(DeliveryStatus status)
    {
        return status switch
        {
            DeliveryStatus.ToBePlanned => "🔴",
            DeliveryStatus.Confirmed => "🟢",
            DeliveryStatus.InProgress => "🟣",
            DeliveryStatus.Delivered => "✅",
            DeliveryStatus.Canceled => "⚫",
            _ => "⚪"
        };
    }

    private static string GetStatusLabel(DeliveryStatus status)
    {
        return status.Humanize();
    }

    private async Task ReloadDeliveriesAsync()
    {
        if (_viewMode == ViewMode.Cards)
        {
            await LoadDeliveriesAsync();
        }
        else
        {
            await _tableRef!.ReloadServerData();
        }
    }
}
