using DropFlow.Domain.Enums;
using DropFlow.Shared.Clients;
using DropFlow.Shared.Deliveries;
using DropFlow.Shared.Routes;
using DropFlow.Shared.Stores;
using DropFlow.Shared.TimeSlots;
using DropFlow.WebApp.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using DropFlow.WebApp.Models.Deliveries;

namespace DropFlow.WebApp.Components.Pages.Deliveries;

public partial class CreateDelivery : IAsyncDisposable
{
    // ════════════════════════════════════════════════════════════════
    // SERVICES INJECTÉS
    // ════════════════════════════════════════════════════════════════

    [Inject] private IDeliveryService DeliveryService { get; set; } = default!;
    [Inject] private IClientService ClientService { get; set; } = default!;
    [Inject] private IStoreService StoreService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject] private IRouteService RouteService { get; set; } = default!;
    [Inject] private ITimeSlotService TimeSlotService { get; set; } = default!;
    // ════════════════════════════════════════════════════════════════
    // PARAMETERS
    // ════════════════════════════════════════════════════════════════

    [Parameter] public int? Id { get; set; }

    // ════════════════════════════════════════════════════════════════
    // STATE
    // ════════════════════════════════════════════════════════════════

    private CreateDeliveryModel _model = new();
    private ClientLookupDto? _selectedClient;
    private List<StoreDto> _stores = new();
    private List<RouteViewDto> _routes = new();
    private List<TimeSlotDto> _timeSlots = new();
    private bool _isAssignedToRoute; // ✅ NOUVEAU
    private bool _isSaving;
    
    // Google Maps Autocomplete
    private MudTextField<string>? _addressField;
    private IJSObjectReference? _googleMapsModule;
    private DotNetObjectReference<CreateDelivery>? _dotNetRef;

    // Breadcrumbs
    private List<BreadcrumbItem> _breadcrumbs = new();

    // ════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ════════════════════════════════════════════════════════════════

    protected override async Task OnInitializedAsync()
    {
        InitializeBreadcrumbs();
        
        try
        {
            // ✅ CHARGER EN PARALLÈLE (au lieu de séquentiel)
            var storesTask = LoadStoresAsync();
            var routesTask = LoadRoutesAsync();
            var timeSlotsTask = LoadTimeSlotsAsync(); // ✅ NOUVEAU

            var deliveryTask = Id.HasValue 
                ? LoadDeliveryAsync(Id.Value) 
                : Task.CompletedTask;
        
            // Attendre que TOUS soient terminés
            await Task.WhenAll(storesTask, routesTask, timeSlotsTask, deliveryTask);
            
            // Valeurs par défaut si création
            if (!Id.HasValue)
            {
                _model.Status = DeliveryStatus.ToBePlanned;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading data: {ex.Message}");
            Snackbar.Add("Erreur lors du chargement des données", Severity.Error);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // ✅ Fire and forget - ne pas attendre Google Maps
            _ = InitializeGoogleMapsAutocompleteAsync();
        }
    }

    // ════════════════════════════════════════════════════════════════
    // CHARGEMENT DONNÉES
    // ════════════════════════════════════════════════════════════════

    private void InitializeBreadcrumbs()
    {
        _breadcrumbs =
        [
            new BreadcrumbItem("Accueil", href: "/"),
            new BreadcrumbItem("Livraisons", href: "/livraisons"),
            new BreadcrumbItem(Id.HasValue ? "Modifier" : "Nouvelle", href: null, disabled: true)
        ];
    }

    private async Task LoadStoresAsync()
    {
        try
        {
            _stores = await StoreService.GetAllStoresAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add("Erreur lors du chargement des enseignes", Severity.Error);
        }
    }

    private async Task LoadRoutesAsync()
    {
        try
        {
            var result = await RouteService.GetRoutesAsync(new RouteFilterDto
            {
                PageSize = 100
            });
            _routes = result?.Items.ToList() ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading routes: {ex.Message}");
            _routes = [];
        }
    }

    private async Task LoadTimeSlotsAsync()
    {
        try
        {
            _timeSlots = await TimeSlotService.GetAllAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading timeslots: {ex.Message}");
            _timeSlots = new List<TimeSlotDto>();
        }
    }
    
    private async Task LoadDeliveryAsync(int id)
    {
        try
        {
            var result = await DeliveryService.GetDeliveryByIdAsync(id);
            
            if (!result.Succeeded || result.Data == null)
            {
                Snackbar.Add("Livraison introuvable", Severity.Error);
                Navigation.NavigateTo("/livraisons");
                return;
            }

            // Mapper DeliveryDto → CreateDeliveryModel
            var delivery = result.Data;
            
            _model = new CreateDeliveryModel
            {
                ClientId = delivery.ClientId,
                ClientAddressId = delivery.ClientAddressId,
                
                ClientFirstName = delivery.Client?.FirstName ?? string.Empty,
                ClientLastName = delivery.Client?.LastName ?? string.Empty,
                ClientEmail = delivery.Client?.Email,
                ClientPhone = delivery.Client?.Phone ?? string.Empty,
                
                Address = delivery.Address ?? string.Empty,
                ZipCode = delivery.ZipCode ?? string.Empty,
                City = delivery.City ?? string.Empty,
                AddressComplement = delivery.AddressComplement,
                
                StoreId = delivery.StoreId,
                FileNumber = delivery.FileNumber,
                ScheduledDate = delivery.ScheduledDate,
                Price = delivery.Price,
                ClientPaymentAmount = delivery.ClientPaymentAmount,
                StorePaymentAmount = delivery.StorePaymentAmount,
                
                Status = delivery.Status,
                WithAssembly = delivery.WithAssembly,
                DeliveryNotes = delivery.DeliveryNotes,
                InternalNotes = delivery.InternalNotes,
                
                EstimatedDurationMinutes = delivery.EstimatedDurationMinutes,
                TimeSlotId = delivery.TimeSlotId,
                
                Items = delivery.Items?.Select(i => new DeliveryItemModel
                {
                    Reference = i.Reference,
                    Designation = i.Designation,
                    Quantity = i.Quantity,
                    Information = i.Information
                }).ToList() ?? [],

                RouteId = delivery.RouteId
            };

            _isAssignedToRoute = delivery.RouteId.HasValue;
        }
        catch (Exception ex)
        {
            Snackbar.Add("Erreur lors du chargement de la livraison", Severity.Error);
            Navigation.NavigateTo("/livraisons");
        }
    }

    // ════════════════════════════════════════════════════════════════
    // AUTOCOMPLETE CLIENT
    // ════════════════════════════════════════════════════════════════

    private async Task<IEnumerable<ClientLookupDto>> SearchClients(string searchTerm, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 3)
        {
            return [];
        }

        try
        {
            var clients = await ClientService.SearchClientsAsync(searchTerm);
            return clients;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching clients: {ex.Message}");
            return [];
        }
    }

    private void OnClientSelected(ClientLookupDto? client)
    {
        _selectedClient = client;

        if (client != null)
        {
            // Stocker l'ID client pour l'envoi au backend
            _model.ClientId = client.Id;
            
            // Extraire FirstName et LastName du DisplayName
            var names = client.DisplayName.Split(' ', 2);
            _model.ClientFirstName = names.Length > 0 ? names[0] : string.Empty;
            _model.ClientLastName = names.Length > 1 ? names[1] : string.Empty;
            _model.ClientEmail = client.Email;
            _model.ClientPhone = client.Phone ?? string.Empty;

            // Pré-remplir avec l'adresse par défaut
            var defaultAddress = client.Addresses.FirstOrDefault(a => a.IsDefault);
            if (defaultAddress != null)
            {
                // Stocker l'ID de l'adresse
                _model.ClientAddressId = defaultAddress.Id;
                
                // Parser FullAddress: "12 rue Example, 51100 Reims"
                var parts = defaultAddress.FullAddress.Split(',');
                if (parts.Length >= 2)
                {
                    _model.Address = parts[0].Trim();
                    var cityParts = parts[1].Trim().Split(' ', 2);
                    if (cityParts.Length >= 2)
                    {
                        _model.ZipCode = cityParts[0];
                        _model.City = cityParts[1];
                    }
                }
            }

            Snackbar.Add($"Client {client.DisplayName} sélectionné", Severity.Success);
        }
        else
        {
            ResetClientForm();
        }

        StateHasChanged();
    }

    private void ResetClientForm()
    {
        _model.ClientId = null;
        _model.ClientAddressId = null;
        _model.ClientFirstName = string.Empty;
        _model.ClientLastName = string.Empty;
        _model.ClientEmail = null;
        _model.ClientPhone = string.Empty;
        _model.Address = string.Empty;
        _model.ZipCode = string.Empty;
        _model.City = string.Empty;
        _model.AddressComplement = null;
    }

    // ════════════════════════════════════════════════════════════════
    // GOOGLE MAPS AUTOCOMPLETE
    // ════════════════════════════════════════════════════════════════

    private async Task InitializeGoogleMapsAutocompleteAsync()
    {
        try
        {
            Console.WriteLine("🗺️ Initializing Google Maps...");

            var retries = 0;
            const int maxRetries = 10; // ✅ 10 × 200ms = 2 secondes max (au lieu de 10s)
            var isGoogleMapsLoaded = false;
        
            while (retries < maxRetries)
            {
                try
                {
                    isGoogleMapsLoaded = await JSRuntime.InvokeAsync<bool>(
                        "eval", 
                        "!!(window.google && window.google.maps && window.google.maps.places)"
                    );
                
                    if (isGoogleMapsLoaded)
                    {
                        Console.WriteLine("✅ Google Maps loaded");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error checking Google Maps: {ex.Message}");
                }
            
                await Task.Delay(200); // ✅ 200ms au lieu de 500ms
                retries++;
            }
        
            if (!isGoogleMapsLoaded)
            {
                Console.WriteLine("⚠️ Google Maps timeout - autocomplete disabled");
                Snackbar.Add("Autocomplete Google Maps non disponible", Severity.Warning);
                return;
            }
        
            // Continuer initialisation
            _dotNetRef = DotNetObjectReference.Create(this);
            _googleMapsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "/js/googleMapsAutocomplete.js");

            await Task.Delay(100);

            await _googleMapsModule.InvokeVoidAsync(
                "initializeAutocomplete",
                _dotNetRef,
                "address-field"
            );
        
            Console.WriteLine("✅ Google Maps autocomplete ready");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Google Maps error: {ex.Message}");
            // Ne pas bloquer l'application si Google Maps échoue
        }
    }

    [JSInvokable]
    public Task OnAddressSelected(GooglePlaceResult place)
    {
        try
        {
            // Réinitialiser ClientAddressId car c'est une nouvelle adresse
            _model.ClientAddressId = null;
            
            _model.Address = place.StreetAddress ?? string.Empty;
            _model.City = place.City ?? string.Empty;
            _model.ZipCode = place.PostalCode ?? string.Empty;

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing address selection: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }
    
    private string? GetHelperTextForScheduledDate()
    {
        if (_model.ScheduledDate.HasValue && _model.ScheduledDate.Value < DateTime.Today)
        {
            return " \u26a0\ufe0f Date dans le passé";
        }

        return null;
    }

    // ════════════════════════════════════════════════════════════════
    // GESTION PRODUITS
    // ════════════════════════════════════════════════════════════════

    private void AddProduct()
    {
        _model.Items.Add(new DeliveryItemModel
        {
            Quantity = 1
        });
    }

    private void RemoveProduct(DeliveryItemModel item)
    {
        _model.Items.Remove(item);
    }

    private void OnScheduledDateChanged(DateTime? date)
    {
        if (date.HasValue && _model.Status != DeliveryStatus.ToBePlanned)
        {
            _model.Status = DeliveryStatus.Confirmed;
            Snackbar.Add("Statut changé automatiquement à 'Confirmée'", Severity.Info);
        }
    }
    
    // ════════════════════════════════════════════════════════════════
    // SUBMIT & VALIDATION
    // ════════════════════════════════════════════════════════════════

    private async Task HandleSubmit()
    {
        // Validation manuelle supplémentaire
        if (_model is { ScheduledDate: not null, EstimatedDurationMinutes: null })
        {
            Snackbar.Add("La durée de prestation est obligatoire lorsque la date de livraison est définie", Severity.Error);
            return;
        }

        // ✅ VALIDATION : Si Confirmed ou InProgress → Date OBLIGATOIRE
        if ((_model.Status == DeliveryStatus.Confirmed || _model.Status == DeliveryStatus.InProgress) 
            && !_model.ScheduledDate.HasValue)
        {
            Snackbar.Add("La date de livraison est obligatoire pour une livraison confirmée ou en cours", Severity.Error);
            return;
        }
        
        if (!ValidateForm())
        {
            return;
        }

        _isSaving = true;
        StateHasChanged();

        try
        {
            if (Id.HasValue)
            {
                // ═══ MODIFICATION ═══
                var updateDto = new UpdateDeliveryDto
                {
                    ClientId = _model.ClientId,
                    ClientAddressId = _model.ClientAddressId,
                    
                    // Si nouveau client (ClientId null)
                    ClientFirstName = _model.ClientFirstName,
                    ClientLastName = _model.ClientLastName,
                    ClientPhone = _model.ClientPhone,
                    ClientEmail = _model.ClientEmail,
                    
                    // Si nouvelle adresse (ClientAddressId null)
                    Address = _model.Address,
                    ZipCode = _model.ZipCode,
                    City = _model.City,
                    AddressComplement = _model.AddressComplement,
                    
                    StoreId = _model.StoreId!.Value,
                    FileNumber = _model.FileNumber,
                    ScheduledDate = _model.ScheduledDate,
                    Price = _model.Price,
                    ClientPaymentAmount = _model.ClientPaymentAmount,
                    StorePaymentAmount = _model.StorePaymentAmount,
                    
                    Status = _model.Status,
                    WithAssembly = _model.WithAssembly,
                    DeliveryNotes = _model.DeliveryNotes,
                    InternalNotes = _model.InternalNotes,
                    EstimatedDurationMinutes = _model.EstimatedDurationMinutes,
                    TimeSlotId = _model.TimeSlotId,
                    
                    Items = _model.Items.Select(i => new UpdateDeliveryItemDto
                    {
                        Reference = i.Reference,
                        Designation = i.Designation,
                        Quantity = i.Quantity,
                        Information = i.Information
                    }).ToList()
                };

                var result = await DeliveryService.UpdateDeliveryAsync(Id.Value, updateDto);
                
                if (result.Succeeded)
                {
                    Snackbar.Add("Livraison modifiée avec succès", Severity.Success);
                    Navigation.NavigateTo("/livraisons");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Snackbar.Add(error, Severity.Error);
                    }
                }
            }
            else
            {
                // ═══ CRÉATION ═══
                var createDto = new CreateDeliveryDto
                {
                    ClientId = _model.ClientId,
                    ClientAddressId = _model.ClientAddressId,
                    
                    // Si nouveau client (ClientId null)
                    ClientFirstName = _model.ClientFirstName,
                    ClientLastName = _model.ClientLastName,
                    ClientPhone = _model.ClientPhone,
                    ClientEmail = _model.ClientEmail,
                    
                    // Si nouvelle adresse (ClientAddressId null)
                    Address = _model.Address,
                    ZipCode = _model.ZipCode,
                    City = _model.City,
                    AddressComplement = _model.AddressComplement,
                    
                    StoreId = _model.StoreId!.Value,
                    FileNumber = _model.FileNumber,
                    ScheduledDate = _model.ScheduledDate,
                    Price = _model.Price,
                    ClientPaymentAmount = _model.ClientPaymentAmount,
                    StorePaymentAmount = _model.StorePaymentAmount,
                    
                    Status = _model.Status,
                    WithAssembly = _model.WithAssembly,
                    DeliveryNotes = _model.DeliveryNotes,
                    InternalNotes = _model.InternalNotes,
                    EstimatedDurationMinutes = _model.EstimatedDurationMinutes,
                    TimeSlotId = _model.TimeSlotId,
                    
                    Items = _model.Items.Select(i => new CreateDeliveryItemDto
                    {
                        Reference = i.Reference,
                        Designation = i.Designation,
                        Quantity = i.Quantity,
                        Information = i.Information
                    }).ToList()
                };

                var result = await DeliveryService.CreateDeliveryAsync(createDto);
                
                if (result.Succeeded)
                {
                    Snackbar.Add("Livraison créée avec succès", Severity.Success);
                    Navigation.NavigateTo("/livraisons");
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving delivery: {ex.Message}");
            Snackbar.Add("Erreur lors de l'enregistrement", Severity.Error);
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    private bool ValidateForm()
    {
        var errors = new List<string>();

        // Validation produits
        if (!_model.Items.Any())
        {
            errors.Add("Veuillez ajouter au moins un produit");
        }
        else
        {
            foreach (var item in _model.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Designation))
                {
                    errors.Add("Tous les produits doivent avoir une désignation");
                    break;
                }
                if (item.Quantity <= 0)
                {
                    errors.Add("La quantité doit être supérieure à 0");
                    break;
                }
            }
        }

        // Validation date si statut ≠ À Planifier
        if (_model.Status != DeliveryStatus.ToBePlanned && !_model.ScheduledDate.HasValue)
        {
            errors.Add("La date de livraison est obligatoire pour ce statut");
        }

        // Validation prix
        if (_model.Price <= 0)
        {
            errors.Add("Le prix doit être supérieur à 0");
        }

        // Afficher les erreurs
        foreach (var error in errors)
        {
            Snackbar.Add(error, Severity.Warning);
        }

        return !errors.Any();
    }

    private void HandleCancel()
    {
        Navigation.NavigateTo("/livraisons");
    }

    // ════════════════════════════════════════════════════════════════
    // CLEANUP
    // ════════════════════════════════════════════════════════════════

    public async ValueTask DisposeAsync()
    {
        if (_googleMapsModule != null)
        {
            try
            {
                await _googleMapsModule.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }

        _dotNetRef?.Dispose();
    }
}