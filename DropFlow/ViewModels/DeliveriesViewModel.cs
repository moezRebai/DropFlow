using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace DropFlow.ViewModels;

public partial class DeliveriesViewModel : ObservableObject
{
    private static readonly string[] Clients =
        ["IKEA", "Maisons du Monde", "Conforama", "BUT", "Habitat", "Fly", "Alinéa"];

    private static readonly string[] Stores =
        ["Lyon", "Paris", "Marseille", "Bordeaux", "Lille", "Toulouse"];

    public static readonly string[] Statuses =
        ["Planned", "Confirmed", "Canceled", "Done"];
    
    // Filtres
    [ObservableProperty] private string? _selectedClient;
    [ObservableProperty] private string? _selectedStore;
    [ObservableProperty] private DateTime? _minDate;
    [ObservableProperty] private DateTime? _maxDate;
    [ObservableProperty] private string? _selectedStatus;
    [ObservableProperty] private string _searchText = string.Empty;
    
    // Vues
    [ObservableProperty] private string _selectedView = "Grid"; // Grid, Table, Kanban
    public bool IsGridView => SelectedView == "Grid";
    public bool IsTableView => SelectedView == "Table";
    public bool IsKanbanView => SelectedView == "Kanban";

    // Pagination
    private const int MaxVisiblePages = 7;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private ObservableCollection<int> _visiblePages = new();
    private const int PageSize = 12;

    // Collections
    [ObservableProperty] private ObservableCollection<DeliveryItemViewModel> _allDeliveries = new();
    [ObservableProperty] private ObservableCollection<DeliveryItemViewModel> _deliveries = new();
    
    // Collections pour la vue Kanban
    [ObservableProperty] private ObservableCollection<DeliveryItemViewModel> _plannedDeliveries = new();
    [ObservableProperty] private ObservableCollection<DeliveryItemViewModel> _confirmedDeliveries = new();
    [ObservableProperty] private ObservableCollection<DeliveryItemViewModel> _canceledDeliveries = new();
    [ObservableProperty] private ObservableCollection<DeliveryItemViewModel> _doneDeliveries = new();

    // Statistiques
    [ObservableProperty] private int _totalDeliveries;
    [ObservableProperty] private int _plannedCount;
    [ObservableProperty] private int _confirmedCount;
    [ObservableProperty] private int _canceledCount;
    [ObservableProperty] private int _doneCount;
    [ObservableProperty] private int _todayDeliveries;

    public string CurrentPageDisplay => $"Page {CurrentPage} / {TotalPages}";
    private int TotalPages => Math.Max(1, (int)Math.Ceiling((double)FilteredDeliveries.Count / PageSize));
    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    // Données filtrées (sans pagination)
    private List<DeliveryItemViewModel> FilteredDeliveries { get; set; } = new();

    // Collections statiques pour les ComboBox
    public ObservableCollection<string> ClientsList { get; } = new(Clients);
    public ObservableCollection<string> StoresList { get; } = new(Stores);
    public ObservableCollection<string> StatusList { get; } = new(Statuses);

    [ObservableProperty] private DeliveryItemViewModel? _selectedDelivery;

    public IRelayCommand SelectDeliveryCommand { get; }
    public IRelayCommand AddNewDeliveryCommand { get; }
    public IRelayCommand GoToFirstPageCommand { get; }
    public IRelayCommand GoToLastPageCommand { get; }
    public IRelayCommand<int> GoToPageCommand { get; }
    
    public DeliveriesViewModel()
    {
        SelectDeliveryCommand = new RelayCommand<DeliveryItemViewModel>(OnSelectDelivery);
        AddNewDeliveryCommand = new RelayCommand(OnAddNewDelivery);
        GoToFirstPageCommand = new RelayCommand(() => GoToPage(1), () => CurrentPage > 1);
        GoToLastPageCommand = new RelayCommand(() => GoToPage(TotalPages), () => CurrentPage < TotalPages);
        GoToPageCommand = new RelayCommand<int>(GoToPage);
        
        GenerateDeliveries(50);
        ApplyFilters();
        UpdateStatistics();
    }
    
    // ═══════════════════════════════════════════════════════════════
    // FILTRES - Trigger automatique quand un filtre change
    // ═══════════════════════════════════════════════════════════════
    partial void OnSelectedClientChanged(string? value)
    {
        ApplyFilters();
    }

    partial void OnSelectedStoreChanged(string? value)
    {
        ApplyFilters();
    }

    partial void OnMinDateChanged(DateTime? value)
    {
        ApplyFilters();
    }

    partial void OnMaxDateChanged(DateTime? value)
    {
        ApplyFilters();
    }

    partial void OnSelectedStatusChanged(string? value)
    {
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedViewChanged(string value)
    {
        OnPropertyChanged(nameof(IsGridView));
        OnPropertyChanged(nameof(IsTableView));
        OnPropertyChanged(nameof(IsKanbanView));
        RefreshPage();
    }

    /// <summary>
    /// Applique tous les filtres actifs et met à jour les collections
    /// </summary>
    private void ApplyFilters()
    {
        var query = AllDeliveries.AsEnumerable();

        // Filtre par client
        if (!string.IsNullOrEmpty(SelectedClient))
            query = query.Where(d => d.ClientName == SelectedClient);

        // Filtre par magasin
        if (!string.IsNullOrEmpty(SelectedStore))
            query = query.Where(d => d.StoreName == SelectedStore);

        // Filtre par statut
        if (!string.IsNullOrEmpty(SelectedStatus))
            query = query.Where(d => d.Status == SelectedStatus);

        // Filtre par date min
        if (MinDate.HasValue)
            query = query.Where(d => d.Date.Date >= MinDate.Value.Date);

        // Filtre par date max
        if (MaxDate.HasValue)
            query = query.Where(d => d.Date.Date <= MaxDate.Value.Date);

        // Filtre par recherche textuelle
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLower();
            query = query.Where(d => 
                d.ClientName.ToLower().Contains(search) ||
                d.StoreName.ToLower().Contains(search));
        }

        FilteredDeliveries = query.ToList();
        
        // Retour à la page 1 après filtrage
        CurrentPage = 1;
        UpdateStatistics();
        RefreshPage();
    }

    /// <summary>
    /// Met à jour les statistiques affichées en haut
    /// </summary>
    private void UpdateStatistics()
    {
        TotalDeliveries = FilteredDeliveries.Count;
        PlannedCount = FilteredDeliveries.Count(d => d.Status == "Planned");
        ConfirmedCount = FilteredDeliveries.Count(d => d.Status == "Confirmed");
        CanceledCount = FilteredDeliveries.Count(d => d.Status == "Canceled");
        DoneCount = FilteredDeliveries.Count(d => d.Status == "Done");
        TodayDeliveries = FilteredDeliveries.Count(d => d.Date.Date == DateTime.Today);
    }

    // ═══════════════════════════════════════════════════════════════
    // PAGINATION
    // ═══════════════════════════════════════════════════════════════
    
    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void NextPage()
    {
        if (!CanGoNext) return;
        CurrentPage++;
        RefreshPage();
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void PreviousPage()
    {
        if (!CanGoPrevious) return;
        CurrentPage--;
        RefreshPage();
    }

    private void RefreshPage()
    {
        if (IsKanbanView)
        {
            RefreshKanbanView();
        }
        else
        {
            RefreshGridOrTableView();
        }

        OnPropertyChanged(nameof(CurrentPageDisplay));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));

        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
        GoToFirstPageCommand.NotifyCanExecuteChanged();
        GoToLastPageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Rafraîchit la vue Kanban (pas de pagination, mais regroupement par statut)
    /// </summary>
    private void RefreshKanbanView()
    {
        PlannedDeliveries.Clear();
        ConfirmedDeliveries.Clear();
        CanceledDeliveries.Clear();
        DoneDeliveries.Clear();

        foreach (var delivery in FilteredDeliveries)
        {
            switch (delivery.Status)
            {
                case "Planned":
                    PlannedDeliveries.Add(delivery);
                    break;
                case "Confirmed":
                    ConfirmedDeliveries.Add(delivery);
                    break;
                case "Canceled":
                    CanceledDeliveries.Add(delivery);
                    break;
                case "Done":
                    DoneDeliveries.Add(delivery);
                    break;
            }
        }
    }

    /// <summary>
    /// Rafraîchit les vues Grid ou Table avec pagination
    /// </summary>
    private void RefreshGridOrTableView()
    {
        Deliveries.Clear();
        
        var items = FilteredDeliveries
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize);
        
        foreach (var item in items)
            Deliveries.Add(item);
    }

    private void GoToPage(int page)
    {
        if (page < 1 || page > TotalPages)
            return;

        CurrentPage = page;
        UpdateVisiblePages();
        RefreshPage();
    }

    private void UpdateVisiblePages()
    {
        VisiblePages.Clear();

        int start = Math.Max(1, CurrentPage - MaxVisiblePages / 2);
        int end = Math.Min(TotalPages, start + MaxVisiblePages - 1);

        if (end - start < MaxVisiblePages - 1)
            start = Math.Max(1, end - MaxVisiblePages + 1);

        for (int i = start; i <= end; i++)
            VisiblePages.Add(i);
    }

    // ═══════════════════════════════════════════════════════════════
    // ACTIONS
    // ═══════════════════════════════════════════════════════════════
    
    [RelayCommand]
    private void ResetFilters()
    {
        SelectedClient = null;
        SelectedStore = null;
        MinDate = null;
        MaxDate = null;
        SelectedStatus = null;
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void SwitchToGrid()
    {
        SelectedView = "Grid";
    }

    [RelayCommand]
    private void SwitchToTable()
    {
        SelectedView = "Table";
    }

    [RelayCommand]
    private void SwitchToKanban()
    {
        SelectedView = "Kanban";
    }

    [RelayCommand]
    private void ExportDeliveries()
    {
        // TODO: Implémenter l'export (Excel, CSV, PDF)
        System.Diagnostics.Debug.WriteLine("Export deliveries");
    }

    [RelayCommand]
    private void RefreshData()
    {
        // TODO: Recharger depuis la base de données
        GenerateDeliveries(50);
        ApplyFilters();
        System.Diagnostics.Debug.WriteLine("Data refreshed");
    }

    private void OnSelectDelivery(DeliveryItemViewModel? delivery)
    {
        if (delivery == null || delivery.IsAddNewCard) return;
        SelectedDelivery = delivery;
        System.Diagnostics.Debug.WriteLine($"Selected delivery: {delivery.ClientName}");
        // TODO: navigate to details or open dialog
    }

    private void OnAddNewDelivery()
    {
        System.Diagnostics.Debug.WriteLine("Add new delivery clicked");
        // TODO: show creation dialog
    }

    // ═══════════════════════════════════════════════════════════════
    // DONNÉES DE TEST
    // ═══════════════════════════════════════════════════════════════
    
    private void GenerateDeliveries(int count)
    {
        var rand = new Random();
        
        // Carte "Ajouter"
        AllDeliveries.Add(new DeliveryItemViewModel
        {
            IsAddNewCard = true
        });

        // Générer des livraisons avec images
        for (var i = 0; i < count; i++)
        {
            var storeName = Stores[rand.Next(Stores.Length)];
            var delivery = new DeliveryItemViewModel
            {
                Id = Guid.NewGuid(),
                ClientName = Clients[rand.Next(Clients.Length)],
                StoreName = storeName,
                Date = DateTime.Now.AddDays(rand.Next(-5, 10)).AddHours(rand.Next(8, 18)),
                Status = Statuses[rand.Next(Statuses.Length)],
                ProductCount = rand.Next(1, 6), // Entre 1 et 5 produits
               ProductImageUrl = ProductImageHelper.GetImageForClient(storeName)
            };

            // Assigner une image aléatoire (ou laisser null pour avoir l'icône par défaut)
            //delivery.ProductImageUrl ??= ProductImageHelper.GetRandomProductImage();

            AllDeliveries.Add(delivery);
        }
    }
}