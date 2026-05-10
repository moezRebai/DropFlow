using DropFlow.Domain.Enums;
using DropFlow.Shared.Deliveries;
using DropFlow.WebApp.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DropFlow.WebApp.Components.Pages.Deliveries;

public partial class DeliveryDetail : ComponentBase
{
    [Parameter] public int Id { get; set; }

    [Inject] private IDeliveryService DeliveryService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private DeliveryDto? _delivery;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var result = await DeliveryService.GetDeliveryByIdAsync(Id);

            if (result.Succeeded && result.Data != null)
                _delivery = result.Data;
            else
                Snackbar.Add("Livraison introuvable", Severity.Error);
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

    private void GoBack() => NavigationManager.NavigateTo("/livraisons");

    private void GoToEdit() => NavigationManager.NavigateTo($"/livraisons/edit/{Id}");

    private static string GetStatusStyle(DeliveryStatus status) => status switch
    {
        DeliveryStatus.ToBePlanned => "background:#FEF3C7; color:#D97706; border-radius:6px;",
        DeliveryStatus.Confirmed   => "background:#DBEAFE; color:#1D4ED8; border-radius:6px;",
        DeliveryStatus.InProgress  => "background:#EDE9FE; color:#7C3AED; border-radius:6px;",
        DeliveryStatus.Delivered   => "background:#D1FAE5; color:#059669; border-radius:6px;",
        DeliveryStatus.Canceled    => "background:#F3F4F6; color:#4B5563; border-radius:6px;",
        _                          => "background:#F3F4F6; color:#6B7280; border-radius:6px;"
    };
}
