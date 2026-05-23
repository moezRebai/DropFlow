using DropFlow.Shared.Clients;
using DropFlow.Shared.Common;
using DropFlow.WebApp.Components.Shared;
using DropFlow.WebApp.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DropFlow.WebApp.Components.Pages.Clients;

public partial class Clients : ComponentBase
{
    [Inject] private IClientService ClientService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private MudTable<ClientDto> _table = default!;
    private List<ClientDto> _clients = [];
    private PagedResult<ClientDto>? _paginatedResult;
    private bool _loading = true;
    private string _searchTerm = string.Empty;
    private int _currentPage = 1;

    // Statistiques
    private int _totalClients;
    private int _vipClients;
    private decimal _totalRevenue;

    /// <summary>
    /// Charge les clients depuis le serveur (appelé automatiquement par MudTable ServerData)
    /// </summary>
    private async Task<TableData<ClientDto>> LoadServerData(TableState state, CancellationToken token)
    {
        _loading = true;

        try
        {
            var filter = new ClientFilterDto
            {
                SearchTerm = _searchTerm,
                Page = state.Page + 1, // MudTable commence à 0, backend commence à 1
                PageSize = state.PageSize
            };

            _paginatedResult = await ClientService.GetClientsAsync(filter);

            if (_paginatedResult != null)
            {
                _clients = _paginatedResult.Items.ToList();
                
                // Calculer les statistiques
                _totalClients = _paginatedResult.TotalCount;
                _vipClients = _clients.Count(IsVip);
                _totalRevenue = _clients.Sum(c => c.TotalRevenue);

                await InvokeAsync(StateHasChanged);

                return new TableData<ClientDto>
                {
                    Items = _paginatedResult.Items,
                    TotalItems = _paginatedResult.TotalCount
                };

            }

            return new TableData<ClientDto> { Items = [], TotalItems = 0 };
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur lors du chargement des clients : {ex.Message}", Severity.Error);
            return new TableData<ClientDto> { Items = [], TotalItems = 0 };
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Charge la liste des clients avec pagination (méthode manuelle gardée pour le bouton Actualiser)
    /// </summary>
    private async Task LoadClients()
    {
        // Recharger via ServerData
        if (_table != null)
        {
            await _table.ReloadServerData();
        }
    }

    /// <summary>
    /// Détermine si un client est VIP (≥ 3 livraisons)
    /// </summary>
    private static bool IsVip(ClientDto client)
    {
        return client.TotalDeliveries >= 3;
    }

    /// <summary>
    /// Gère le changement de recherche
    /// </summary>
    private async Task OnSearchChanged()
    {
        // Recharger la table (retour à la page 1 automatique)
        if (_table != null)
        {
            await _table.ReloadServerData();
        }
    }

    /// <summary>
    /// Gère le changement de page (utilisé par la pagination manuelle si nécessaire)
    /// </summary>
    private async Task OnPageChanged(int page)
    {
        _currentPage = page;
        if (_table != null)
        {
            await _table.ReloadServerData();
        }
    }

    /// <summary>
    /// Ouvre le dialog de détails du client
    /// </summary>
    private async Task OpenDetailDialog(int clientId)
    {
        var parameters = new DialogParameters<ClientDetailDialog>
        {
            { x => x.ClientId, clientId }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<ClientDetailDialog>("Détails Client", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            // Recharger la liste si modification
            if (_table != null)
            {
                await _table.ReloadServerData();
            }
        }
    }

    /// <summary>
    /// Ouvre le dialog d'édition du client
    /// </summary>
    private async Task OpenEditDialog(int clientId)
    {
        var parameters = new DialogParameters<EditClientDialog>
        {
            { x => x.ClientId, clientId }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<EditClientDialog>("Modifier Client", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            Snackbar.Add("Client modifié avec succès", Severity.Success);
            if (_table != null)
            {
                await _table.ReloadServerData();
            }
        }
    }

    /// <summary>
    /// Supprime un client (avec vérification des livraisons côté backend)
    /// </summary>
    private async Task DeleteClient(int clientId, string clientName)
    {
        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, $"Êtes-vous sûr de vouloir supprimer le client \"{clientName}\" ?" },
            { x => x.ButtonText, "Supprimer" },
            { x => x.Color, Color.Error }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirmation", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            try
            {
                var deleteResult = await ClientService.DeleteClientAsync(clientId);

                if (deleteResult.Succeeded)
                {
                    Snackbar.Add("Client supprimé avec succès", Severity.Success);
                    if (_table != null)
                    {
                        await _table.ReloadServerData();
                    }
                }
                else
                {
                    // Backend vérifie les livraisons
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
}
