using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace DropFlow.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    // KPI Metrics avec tendances
    [ObservableProperty] private int _nonPlannedDeliveries = 5;
    [ObservableProperty] private string _nonPlannedTrend = "+2";
    [ObservableProperty] private bool _nonPlannedIsUp = false;
    
    [ObservableProperty] private int _deliveriesToday = 12;
    [ObservableProperty] private string _deliveriesTodayTrend = "+15%";
    [ObservableProperty] private bool _deliveriesTodayIsUp = true;
    
    [ObservableProperty] private int _pendingInvoices = 3;
    [ObservableProperty] private string _pendingInvoicesTrend = "-1";
    [ObservableProperty] private bool _pendingInvoicesIsUp;
    
    [ObservableProperty] private decimal _revenueSummary = 1250.75m;
    [ObservableProperty] private string _revenueTrend = "+8.5%";
    [ObservableProperty] private bool _revenueIsUp = true;
    
    // UI State
    [ObservableProperty] private bool _controlsEnabled = true;
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private DateTime _lastSyncTime = DateTime.Now;
    [ObservableProperty] private string _status = "Connected";
    [ObservableProperty] private int _unreadNotifications = 3;
    
    // Chart Period Selection
    [ObservableProperty] private string _selectedPeriod = "Année";
    public ObservableCollection<string> ChartPeriods { get; } = ["Semaine", "Mois", "Année"];

    public ObservableCollection<Delivery> UpcomingDeliveries { get; } =
    [
        new Delivery("Client A", "123 Main St", DateTime.Today.AddHours(10), "Planned", "#4CAF50"),
        new Delivery("Client B", "456 Oak Ave", DateTime.Today.AddHours(12), "Planned", "#4CAF50"),
        new Delivery("Client C", "789 Pine Rd", DateTime.Today.AddHours(14), "Late", "#F44336")
    ];

    public ObservableCollection<AlertMessage> Alerts { get; } =
    [
        new AlertMessage("CheckCircle", "Route sync completed", DateTime.Now.AddMinutes(-15), "Success"),
        new AlertMessage("AlertCircle", "Invoice #123 overdue", DateTime.Now.AddMinutes(-30), "Warning"),
        new AlertMessage("CloseCircle", "Delivery C delayed", DateTime.Now.AddMinutes(-5), "Error")
    ];

    // Quick Actions avec icônes et couleurs
    public ObservableCollection<QuickAction> QuickActions { get; } =
    [
        new QuickAction("Nouvelle Livraison", "TruckPlus", "#10B981", "Créer une livraison"),
        new QuickAction("Envoyer Factures", "EmailFast", "#F59E0B", "Envoi groupé"),
        new QuickAction("Importer Factures", "FileImport", "#EF4444", "Import Excel/PDF"),
        new QuickAction("Feuille de route", "MapMarkerPath", "#3B82F6", "Optimiser trajet")
    ];

    public IEnumerable<ISeries> Series { get; set; }
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }
    public ICommand SyncCommand { get; }
    public ICommand NewDeliveryCommand { get; }
    public ICommand SendInvoicesCommand { get; }
    public ICommand ImportInvoiceCommand { get; }
    public ICommand RouteSheetCommand { get; }
    public ICommand ChangePeriodCommand { get; }
    public ICommand DismissAlertCommand { get; }
    public ICommand ViewAllNotificationsCommand { get; }

    public HomeViewModel()
    {
        SyncCommand = new RelayCommand(OnSync, () => ControlsEnabled);
        NewDeliveryCommand = new RelayCommand(OnNewDelivery);
        SendInvoicesCommand = new RelayCommand(OnSendInvoices);
        ImportInvoiceCommand = new RelayCommand(OnImportInvoices);
        RouteSheetCommand = new RelayCommand(OnRouteSheet);
        ChangePeriodCommand = new RelayCommand<string>(OnChangePeriod);
        DismissAlertCommand = new RelayCommand<AlertMessage>(OnDismissAlert);
        ViewAllNotificationsCommand = new RelayCommand(OnViewAllNotifications);

        InitializeChart();
    }

    private void InitializeChart()
    {
        var months = new[] { "Oct", "Nov", "Dec", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep" };
        double[] revenues = { 120, 130, 140, 110, 125, 135, 150, 155, 160, 158, 145, 150 };
        double[] deliveries = { 10, 34, 36, 31, 33, 35, 40, 41, 43, 42, 39, 40 };

        // Couleurs modernes - utilisation de SolidColorPaint avec transparence
        var emerald500 = SKColor.Parse("#10B981");
        var blue500 = SKColor.Parse("#3B82F6");

        Series = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Revenue (k€)",
                Values = revenues,
                Fill = new SolidColorPaint(emerald500),
                Stroke = null,
                Rx = 8, // Coins arrondis
                Ry = 8
            },
            new ColumnSeries<double>
            {
                Name = "Livraisons",
                Values = deliveries,
                Fill = new SolidColorPaint(blue500),
                Stroke = null,
                Rx = 8,
                Ry = 8
            }
        };

        XAxes = new[]
        {
            new Axis
            {
                Labels = months,
                LabelsRotation = 0,
                TextSize = 13,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
            }
        };

        YAxes = new[]
        {
            new Axis
            {
                MinLimit = 0,
                TextSize = 13,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
            }
        };
    }

    private async void OnSync()
    {
        IsLoading = true;
        ControlsEnabled = false;
        
        // Simuler sync
        await Task.Delay(2000);
        
        LastSyncTime = DateTime.Now;
        IsLoading = false;
        ControlsEnabled = true;
        
        // Ajouter notification de succès
        Alerts.Insert(0, new AlertMessage("CheckCircle", "Synchronisation réussie", DateTime.Now, "Success"));
        UnreadNotifications++;
    }

    private void OnChangePeriod(string period)
    {
        SelectedPeriod = period;
        // Recharger les données du graphique selon la période
        InitializeChart();
    }

    private void OnDismissAlert(AlertMessage alert)
    {
        if (alert != null)
        {
            Alerts.Remove(alert);
            UnreadNotifications = Math.Max(0, UnreadNotifications - 1);
        }
    }

    private void OnViewAllNotifications()
    {
        // Navigation vers page notifications
    }

    private void OnNewDelivery() { /* navigation / dialog logic */ }
    private void OnSendInvoices() { /* send invoices logic */ }
    private void OnImportInvoices() { /* import invoices logic */ }
    private void OnRouteSheet() { /* open route sheet */ }
}

public class Delivery
{
    public string ClientName { get; set; }
    public string Address { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; }
    public string StatusColor { get; set; }

    public Delivery(string client, string address, DateTime time, string status, string color)
    {
        ClientName = client;
        Address = address;
        ScheduledTime = time;
        Status = status;
        StatusColor = color;
    }
}

public class AlertMessage
{
    public string Icon { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public string Type { get; set; }

    public AlertMessage(string icon, string message, DateTime timestamp, string type)
    {
        Icon = icon;
        Message = message;
        Timestamp = timestamp;
        Type = type;
    }

    public string Color => Type switch
    {
        "Error" => "#EF4444",
        "Warning" => "#F59E0B",
        "Success" => "#10B981",
        _ => "#3B82F6"
    };

    public string BackgroundColor => Type switch
    {
        "Error" => "#FEE2E2",
        "Warning" => "#FEF3C7",
        "Success" => "#D1FAE5",
        _ => "#DBEAFE"
    };
}

public class QuickAction(string title, string icon, string color, string description)
{
    public string Title { get; set; } = title;
    public string Icon { get; set; } = icon;
    public string Color { get; set; } = color;
    public string Description { get; set; } = description;
}