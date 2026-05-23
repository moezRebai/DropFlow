using DropFlow.Shared.Clients;
using DropFlow.WebApp.Components.Shared;
using DropFlow.WebApp.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DropFlow.WebApp.Components.Pages.Clients;

public partial class EditClientDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int ClientId { get; set; }

    [Inject] private IClientService ClientService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private ClientDto? _client;
    private UpdateClientDto _model = new();
    private List<ClientAddressDto> _addresses = [];
    
    private MudForm _form = default!;
    private bool _isValid;
    private bool _loading = true;
    private bool _saving;

    protected override async Task OnInitializedAsync()
    {
        await LoadClient();
    }

    /// <summary>
    /// Charge le client et ses adresses
    /// </summary>
    private async Task LoadClient()
    {
        _loading = true;

        try
        {
            _client = await ClientService.GetClientByIdAsync(ClientId);

            if (_client == null)
            {
                Snackbar.Add("Client introuvable", Severity.Error);
                return;
            }

            // Remplir le modèle
            _model = new UpdateClientDto
            {
                FirstName = _client.FirstName,
                LastName = _client.LastName,
                Phone = _client.Phone,
                Email = _client.Email ?? string.Empty,
                IsActive = _client.IsActive
            };

            // Charger les adresses
            _addresses = await ClientService.GetClientAddressesAsync(ClientId);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur : {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Enregistre les modifications du client
    /// </summary>
    private async Task Save()
    {
        await _form.Validate();

        if (!_isValid)
        {
            Snackbar.Add("Veuillez corriger les erreurs", Severity.Warning);
            return;
        }

        _saving = true;

        try
        {
            var result = await ClientService.UpdateClientAsync(ClientId, _model);

            if (result.Succeeded)
            {
                Snackbar.Add("Client modifié avec succès", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                var errorMessage = result.Errors?.FirstOrDefault() ?? "Erreur lors de la mise à jour";
                Snackbar.Add(errorMessage, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur : {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    /// <summary>
    /// Annule les modifications
    /// </summary>
    private void Cancel()
    {
        MudDialog.Cancel();
    }

    #region Gestion des adresses

    /// <summary>
    /// Ouvre le dialog d'ajout d'adresse
    /// </summary>
    private async Task OpenAddAddressDialog()
    {
        var parameters = new DialogParameters<AddressDialog>
        {
            { x => x.ClientId, ClientId },
            { x => x.IsEdit, false }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<AddressDialog>("Ajouter une adresse", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            Snackbar.Add("Adresse ajoutée avec succès", Severity.Success);
            
            // Recharger les adresses
            _addresses = await ClientService.GetClientAddressesAsync(ClientId);
            StateHasChanged();
        }
    }

    /// <summary>
    /// Ouvre le dialog de modification d'adresse
    /// </summary>
    private async Task OpenEditAddressDialog(ClientAddressDto address)
    {
        var parameters = new DialogParameters<AddressDialog>
        {
            { x => x.ClientId, ClientId },
            { x => x.AddressId, address.Id },
            { x => x.IsEdit, true },
            { x => x.ExistingAddress, address }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<AddressDialog>("Modifier l'adresse", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            Snackbar.Add("Adresse modifiée avec succès", Severity.Success);
            
            // Recharger les adresses
            _addresses = await ClientService.GetClientAddressesAsync(ClientId);
            StateHasChanged();
        }
    }

    /// <summary>
    /// Définit une adresse comme par défaut
    /// </summary>
    private async Task SetDefaultAddress(int addressId)
    {
        try
        {
            var result = await ClientService.SetDefaultAddressAsync(ClientId, addressId);

            if (result.Succeeded)
            {
                Snackbar.Add("Adresse par défaut mise à jour", Severity.Success);
                
                // Recharger les adresses
                _addresses = await ClientService.GetClientAddressesAsync(ClientId);
                StateHasChanged();
            }
            else
            {
                var errorMessage = result.Errors?.FirstOrDefault() ?? "Erreur";
                Snackbar.Add(errorMessage, Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur : {ex.Message}", Severity.Error);
        }
    }

    /// <summary>
    /// Supprime une adresse
    /// </summary>
    private async Task DeleteAddress(int addressId)
    {
        var parameters = new DialogParameters<ConfirmationDialog>
        {
            { x => x.ContentText, "Êtes-vous sûr de vouloir supprimer cette adresse ?" },
            { x => x.ButtonText, "Supprimer" },
            { x => x.Color, Color.Error }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("Confirmation", parameters, options);
        var confirmResult = await dialog.Result;

        if (confirmResult is { Canceled: false })
        {
            try
            {
                var result = await ClientService.DeleteAddressAsync(ClientId, addressId);

                if (result.Succeeded)
                {
                    Snackbar.Add("Adresse supprimée avec succès", Severity.Success);
                    
                    // Recharger les adresses
                    _addresses = await ClientService.GetClientAddressesAsync(ClientId);
                    StateHasChanged();
                }
                else
                {
                    var errorMessage = result.Errors?.FirstOrDefault() ?? "Erreur lors de la suppression";
                    Snackbar.Add(errorMessage, Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Erreur : {ex.Message}", Severity.Error);
            }
        }
    }

    #endregion
}
