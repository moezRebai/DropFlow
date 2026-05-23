using DropFlow.Shared.Enums;
using DropFlow.Shared.Clients;
using DropFlow.Shared.Deliveries;
using DropFlow.WebApp.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DropFlow.WebApp.Components.Pages.Clients;

public partial class ClientDetailDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int ClientId { get; set; }

    [Inject] private IClientService ClientService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private ClientDto? _client;
    private List<DeliveryDto> _deliveries = [];
    private bool _loading = true;

    // Données graphique MudChart
    private List<ChartSeries> _chartSeries = [];
    private string[] _chartLabels = [];
    private ChartOptions _chartOptions = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadClientDetails();
    }

    /// <summary>
    /// Charge les détails du client et son historique
    /// </summary>
    private async Task LoadClientDetails()
    {
        _loading = true;

        try
        {
            // Charger le client
            _client = await ClientService.GetClientByIdAsync(ClientId);

            if (_client == null)
            {
                Snackbar.Add("Client introuvable", Severity.Error);
                return;
            }

            // Charger l'historique des livraisons
            _deliveries = await ClientService.GetClientDeliveriesAsync(ClientId);

            // Préparer les données du graphique
            PrepareChartData();
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
    /// Prépare les données pour le graphique mensuel MudChart (LINE)
    /// </summary>
    private void PrepareChartData()
    {
        if (!_deliveries.Any()) return;

        // Filtrer les livraisons avec une date planifiée
        var deliveriesWithDate = _deliveries
            .Where(d => d.ScheduledDate.HasValue)
            .ToList();

        if (!deliveriesWithDate.Any()) return;

        // Grouper par mois
        var monthlyData = deliveriesWithDate
            .GroupBy(d => new { d.ScheduledDate!.Value.Year, d.ScheduledDate.Value.Month })
            .Select(g => new
            {
                Month = $"{GetMonthName(g.Key.Month)} {g.Key.Year}",
                Count = g.Count(),
                Order = g.Key.Year * 100 + g.Key.Month
            })
            .OrderBy(m => m.Order)
            .TakeLast(12) // 12 derniers mois
            .ToList();

        // Si aucune donnée après filtrage
        if (!monthlyData.Any()) return;

        // Préparer les labels (mois)
        _chartLabels = monthlyData.Select(m => m.Month).ToArray();

        // Préparer les données (nombre de livraisons)
        _chartSeries = new List<ChartSeries>
        {
            new ChartSeries
            {
                Name = "Livraisons",
                Data = monthlyData.Select(m => (double)m.Count).ToArray()
            }
        };

        // Options du graphique avec labels plus lisibles
        _chartOptions = new ChartOptions
        {
            ShowLegend = true,
            YAxisTicks = 4,
            MaxNumYAxisTicks = 10,
            YAxisLines = true,
            XAxisLines = false,
            ChartPalette = ["#1976D2"], // Bleu MudBlazor
            LineStrokeWidth = 4, // Ligne plus épaisse pour meilleure visibilité
            InterpolationOption = InterpolationOption.Straight, // Lignes droites (pas de courbes)
            // Les labels sont automatiquement affichés par MudChart
            // Pour améliorer la lisibilité, on utilise un format court dans GetMonthName
        };
    }

    /// <summary>
    /// Obtient le nom du mois en français
    /// </summary>
    private string GetMonthName(int month)
    {
        return month switch
        {
            1 => "Jan",
            2 => "Fév",
            3 => "Mar",
            4 => "Avr",
            5 => "Mai",
            6 => "Juin",
            7 => "Juil",
            8 => "Aoû",
            9 => "Sep",
            10 => "Oct",
            11 => "Nov",
            12 => "Déc",
            _ => ""
        };
    }

    /// <summary>
    /// Calcule le panier moyen
    /// </summary>
    private string GetAverageBasket()
    {
        if (_client == null || _client.TotalDeliveries == 0)
            return "0";

        var average = _client.TotalRevenue / _client.TotalDeliveries;
        return average.ToString("N0");
    }

    /// <summary>
    /// Détermine si le client est VIP (≥ 3 livraisons)
    /// </summary>
    private bool IsVip()
    {
        return _client != null && _client.TotalDeliveries >= 3;
    }

    /// <summary>
    /// Retourne un chip coloré selon le statut de livraison
    /// </summary>
    private static string GetStatusEmoji(DeliveryStatus status)
    {
        return status switch
        {
            DeliveryStatus.ToBePlanned => "🔴 À Planifier",
            DeliveryStatus.Confirmed => "🟢 Confirmée",
            DeliveryStatus.InProgress => "🟣 En cours",
            DeliveryStatus.Delivered => "✅ Livrée",
            DeliveryStatus.Canceled => "⚫ Annulée",
            _ => "⚪"
        };
    }

    /// <summary>
    /// Ouvre le dialog d'édition
    /// </summary>
    private async Task OpenEditDialog()
    {
        var parameters = new DialogParameters<EditClientDialog>
        {
            { x => x.ClientId, ClientId }
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
            
            // Recharger les détails
            await LoadClientDetails();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Ferme le dialog
    /// </summary>
    private void Close()
    {
        MudDialog.Close(DialogResult.Ok(true));
    }
}
