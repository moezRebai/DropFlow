using DropFlow.Shared.Tenants.Depots;
using DropFlow.Shared.Vehicles;
using DropFlow.WebApp.Interfaces;
using DropFlow.WebApp.Models.Deliveries;
using DropFlow.WebApp.Models.Routes;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace DropFlow.WebApp.Components.Pages.Routes.Steps;

public partial class RouteInfoStep : IAsyncDisposable
{
    [Inject] private IVehicleService VehicleService { get; set; } = default!;
    [Inject] private IRouteService RouteService { get; set; } = default!;
    [Inject] private ITenantManagementService TenantManagementService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILogger<RouteInfoStep> Logger { get; set; } = default!;
    [Parameter] public RouteWizardState State { get; set; } = null!;
    [Parameter] public EventCallback<bool> OnValidationChanged { get; set; }

    // Véhicules
    private List<VehicleDto> _vehicles = [];
    private VehicleDto? _selectedVehicle;

    // Dépôts
    private List<DepotOption> _depotOptions = [];
    private DepotOption? _selectedDepot;
    private bool _isCustomAddress;

    // UI State
    private bool _loading = true;
    private MudTextField<string>? _addressField;
    private IJSObjectReference? _googleMapsModule;
    private DotNetObjectReference<RouteInfoStep>? _dotNetRef;

    // Compteur de livraisons
    private int? _availableDeliveriesCount;
    private bool _checkingDeliveries;
    private DateTime? _lastCheckedDate;

    private bool IsValid =>
        State.VehicleId.HasValue &&
        !string.IsNullOrWhiteSpace(State.DepartureAddress) &&
        State.Date >= DateTime.Today &&
        _availableDeliveriesCount > 0;

    protected override async Task OnInitializedAsync()
    {
        await LoadVehicles();
        await LoadDepots();
        
        // Vérifier les livraisons disponibles pour la date actuelle
        if (State.Date != default)
        {
            await CheckAvailableDeliveriesAsync(State.Date);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Initialiser l'autocomplete Google uniquement si mode saisie manuelle
            if (_isCustomAddress)
            {
                await InitializeGoogleMapsAutocompleteAsync();
            }
        }
    }
    
    public void OnStepEntered()
    {
        // Appelé quand on arrive sur ce step
    }

    public Task<bool> ValidateAsync()
    {
        if (IsValid) return Task.FromResult(true);

        // Messages d'erreur explicites
        if (!State.VehicleId.HasValue)
        {
            Snackbar.Add("Veuillez sélectionner un véhicule", Severity.Warning);
        }
        else if (string.IsNullOrWhiteSpace(State.DepartureAddress))
        {
            Snackbar.Add("Veuillez saisir l'adresse de départ", Severity.Warning);
        }
        else if (State.Date < DateTime.Today)
        {
            Snackbar.Add("La date ne peut pas être dans le passé", Severity.Warning);
        }
        else if (_availableDeliveriesCount == 0)
        {
            Snackbar.Add("Aucune livraison disponible pour cette date. Veuillez choisir une autre date.", Severity.Warning);
        }

        return Task.FromResult(false);
    }

    #region Chargement des données

    private async Task LoadVehicles()
    {
        _loading = true;

        try
        {
            _vehicles = await VehicleService.GetAvailableVehiclesAsync(DateTime.Today);

            // Si véhicule déjà sélectionné (mode edit), le retrouver
            if (State.VehicleId.HasValue)
            {
                _selectedVehicle = _vehicles.FirstOrDefault(v => v.Id == State.VehicleId.Value);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Erreur lors du chargement des véhicules: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task LoadDepots()
    {
        try
        {
            Logger.LogInformation("?? Loading depots...");

            var depots = await TenantManagementService.GetAllDepotsAsync();
            var activeDepots = depots.Where(d => d.IsActive).ToList();

            // Créer les options de dépôts
            _depotOptions = activeDepots
                .OrderByDescending(d => d.IsDefault) // Dépôt par défaut en premier
                .ThenBy(d => d.Name)
                .Select(d => new DepotOption
                {
                    Depot = d,
                    IsDefault = d.IsDefault,
                    DisplayText = d.Name,
                    Address = d.FormattedAddress,
                    IsCustomAddress = false
                })
                .ToList();

            // Ajouter l'option "Autre adresse" à la fin
            _depotOptions.Add(new DepotOption
            {
                Depot = null,
                IsDefault = false,
                DisplayText = "?? Autre adresse (saisie manuelle)",
                Address = string.Empty,
                IsCustomAddress = true
            });

            // Sélection automatique
            if (State.DepotId.HasValue)
            {
                // Mode édition : restaurer le dépôt sélectionné par ID
                _selectedDepot = _depotOptions.FirstOrDefault(o => o.Depot?.Id == State.DepotId.Value);
                
                if (_selectedDepot != null)
                {
                    _isCustomAddress = false;
                    Logger.LogInformation($"? Depot restored in edit mode by ID: {_selectedDepot.DisplayText}");
                }
                else
                {
                    // Le dépôt n'existe plus (supprimé ou désactivé)
                    _isCustomAddress = true;
                    _selectedDepot = _depotOptions.First(o => o.IsCustomAddress);
                    Logger.LogInformation($"?? Depot ID {State.DepotId.Value} not found - switching to custom address");
                }
            }
            else if (!string.IsNullOrWhiteSpace(State.DepartureAddress))
            {
                // Pas de DepotId mais adresse définie ? essayer plusieurs stratégies de matching
                Logger.LogInformation($"?? Trying to detect depot from address: {State.DepartureAddress}");
                
                // Stratégie 1 : Comparaison exacte (insensible à la casse)
                var matchingDepot = _depotOptions.FirstOrDefault(o => 
                    o.Depot != null && 
                    !string.IsNullOrWhiteSpace(o.Depot.FormattedAddress) &&
                    o.Depot.FormattedAddress.Equals(State.DepartureAddress, StringComparison.OrdinalIgnoreCase));
                
                // Stratégie 2 : Si pas de match exact, essayer par coordonnées GPS (si disponibles)
                if (matchingDepot == null && State.DepartureLatitude.HasValue && State.DepartureLongitude.HasValue)
                {
                    Logger.LogInformation($"?? Trying GPS matching: {State.DepartureLatitude}, {State.DepartureLongitude}");
                    
                    matchingDepot = _depotOptions.FirstOrDefault(o =>
                        o.Depot is { HasGpsCoordinates: true } &&
                        Math.Abs(o.Depot.Latitude!.Value - State.DepartureLatitude.Value) < 0.0001 &&
                        Math.Abs(o.Depot.Longitude!.Value - State.DepartureLongitude.Value) < 0.0001);
                    
                    if (matchingDepot != null)
                    {
                        Logger.LogInformation($"? Depot matched by GPS coordinates");
                    }
                }
                
                // Stratégie 3 : Si toujours pas de match, essayer par similarité partielle
                if (matchingDepot == null)
                {
                    Logger.LogInformation($"?? Trying partial address matching...");
                    
                    // Normaliser les adresses pour la comparaison
                    var normalizedAddress = NormalizeAddress(State.DepartureAddress);
                    
                    matchingDepot = _depotOptions.FirstOrDefault(o =>
                        o.Depot != null &&
                        !string.IsNullOrWhiteSpace(o.Depot.FormattedAddress) &&
                        NormalizeAddress(o.Depot.FormattedAddress).Contains(normalizedAddress, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingDepot != null)
                    {
                        Logger.LogInformation($"? Depot matched by partial address similarity");
                    }
                }
                
                if (matchingDepot != null)
                {
                    // Adresse correspond à un dépôt existant
                    _selectedDepot = matchingDepot;
                    _isCustomAddress = false;
                    Logger.LogInformation($"? Depot auto-selected from address: {matchingDepot.DisplayText}");
                }
                else
                {
                    // Adresse personnalisée qui ne correspond à aucun dépôt
                    _isCustomAddress = true;
                    _selectedDepot = _depotOptions.First(o => o.IsCustomAddress);
                    Logger.LogInformation($"? Custom address mode - Address: {State.DepartureAddress}");
                }
            }
            else
            {
                // Aucune sélection : sélectionner le dépôt par défaut s'il existe
                var defaultDepot = _depotOptions.FirstOrDefault(o => o.IsDefault);
                if (defaultDepot != null)
                {
                    _selectedDepot = defaultDepot;
                    _isCustomAddress = false;
                    State.DepotId = defaultDepot.Depot!.Id;
                    State.DepartureAddress = defaultDepot.Depot.FormattedAddress;
                    
                    if (defaultDepot.Depot.HasGpsCoordinates)
                    {
                        State.DepartureLatitude = defaultDepot.Depot.Latitude!.Value;
                        State.DepartureLongitude = defaultDepot.Depot.Longitude!.Value;
                    }
                    
                    Logger.LogInformation($"? Default depot auto-selected: {defaultDepot.DisplayText}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"? Error loading depots: {ex.Message}");
            Snackbar.Add("Erreur lors du chargement des dépôts", Severity.Error);
        }
    }

    #endregion

    #region Autocompletes

    private Task<IEnumerable<VehicleDto>> SearchVehicles(string search, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(search))
            return Task.FromResult(_vehicles.AsEnumerable());

        var filtered = _vehicles.Where(v =>
            v.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            v.PlateNumber.Contains(search, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(filtered);
    }

    private Task<IEnumerable<DepotOption>> SearchDepots(string search, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(search))
            return Task.FromResult(_depotOptions.AsEnumerable());

        var filtered = _depotOptions.Where(d =>
            d.DisplayText.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            d.Address.Contains(search, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(filtered);
    }

    #endregion

    #region Event Handlers

    private async Task OnVehicleChanged(VehicleDto? vehicle)
    {
        // ? SAUVEGARDER LE VÉHICULE PRÉCÉDENT POUR COMPARAISON
        var previousVehicleId = State.VehicleId;
        
        _selectedVehicle = vehicle;

        if (vehicle != null)
        {
            State.VehicleId = vehicle.Id;
            State.VehicleName = vehicle.DisplayName;
            
            Logger.LogInformation($"?? Vehicle selected: {vehicle.DisplayName}");
            
            // ? MARQUER COMME CHANGÉ SI VÉHICULE DIFFÉRENT (en mode édition)
            if (previousVehicleId.HasValue && previousVehicleId.Value != vehicle.Id)
            {
                State.MarkAsChanged();
                Logger.LogInformation($"?? Vehicle changed from {previousVehicleId} to {vehicle.Id} - optimization invalidated");
            }
        }
        else
        {
            State.VehicleId = null;
            State.VehicleName = null;
            Logger.LogInformation("?? Vehicle cleared");
        }

        await OnFieldChanged();
    }

    private void ClearVehicle()
    {
        _selectedVehicle = null;
        State.VehicleId = null;
        State.VehicleName = null;
        _ = OnFieldChanged();
    }

    private async Task OnDepotChanged(DepotOption? option)
    {
        if (option == null)
        {
            _selectedDepot = null;
            _isCustomAddress = false;
            return;
        }

        // ? SAUVEGARDER L'ADRESSE PRÉCÉDENTE POUR COMPARAISON
        var previousAddress = State.DepartureAddress;
        var previousLatitude = State.DepartureLatitude;
        var previousLongitude = State.DepartureLongitude;
        
        _selectedDepot = option;

        if (option.IsCustomAddress)
        {
            // Mode saisie manuelle
            _isCustomAddress = true;
            State.DepotId = null;
            State.DepartureAddress = string.Empty;
            State.DepartureLatitude = null;
            State.DepartureLongitude = null;

            Logger.LogInformation("?? Switched to custom address mode");

            // Initialiser Google Maps après le rendu
            await Task.Delay(100);
            await InitializeGoogleMapsAutocompleteAsync();
        }
        else if (option.Depot != null)
        {
            // Dépôt sélectionné
            _isCustomAddress = false;
            State.DepotId = option.Depot.Id;
            State.DepartureAddress = option.Depot.FormattedAddress;
            
            if (option.Depot.HasGpsCoordinates)
            {
                State.DepartureLatitude = option.Depot.Latitude!.Value;
                State.DepartureLongitude = option.Depot.Longitude!.Value;
            }
            else
            {
                State.DepartureLatitude = null;
                State.DepartureLongitude = null;
            }

            Logger.LogInformation($"? Depot selected: {option.Depot.Name} - {option.Depot.FormattedAddress}");
            
            // ? MARQUER COMME CHANGÉ SI ADRESSE/COORDONNÉES DIFFÉRENTES (en mode édition)
            var addressChanged = !string.IsNullOrEmpty(previousAddress) && 
                                 previousAddress != State.DepartureAddress;
            
            var coordinatesChanged = (previousLatitude.HasValue || previousLongitude.HasValue) &&
                                     (previousLatitude != State.DepartureLatitude || 
                                      previousLongitude != State.DepartureLongitude);
            
            if (addressChanged || coordinatesChanged)
            {
                State.MarkAsChanged();
                Logger.LogInformation($"?? Depot changed - optimization invalidated");
                Logger.LogInformation($"   Previous: {previousAddress}");
                Logger.LogInformation($"   Current:  {State.DepartureAddress}");
            }
        }

        await OnFieldChanged();
    }

    private async Task OnDateChanged(DateTime? newDate)
    {
        if (newDate.HasValue)
        {
            // ? SAUVEGARDER LA DATE PRÉCÉDENTE
            var previousDate = State.Date;
            
            State.Date = newDate.Value;
            await CheckAvailableDeliveriesAsync(newDate.Value);
            
            // ? MARQUER COMME CHANGÉ SI DATE DIFFÉRENTE (en mode édition)
            if (previousDate.HasValue && previousDate.Value.Date != newDate.Value.Date)
            {
                State.MarkAsChanged();
                Logger.LogInformation($"?? Date changed from {previousDate:dd/MM/yyyy} to {newDate:dd/MM/yyyy} - optimization invalidated");
            }
            
            await OnFieldChanged();
        }
    }

    private async Task OnFieldChanged(DateTime? date = null, TimeSpan? timeSpan = null)
    {
        if (date.HasValue)
            State.Date = date.Value;
        
        if (timeSpan.HasValue)
            State.StartTime = timeSpan.Value;

        await OnValidationChanged.InvokeAsync(IsValid);
    }

    #endregion

    #region Vérification livraisons

    private async Task CheckAvailableDeliveriesAsync(DateTime? date)
    {
        if (!date.HasValue) return;

        // Éviter les appels répétés pour la même date
        if (_lastCheckedDate == date && _availableDeliveriesCount.HasValue)
        {
            return;
        }

        _checkingDeliveries = true;
        _lastCheckedDate = date;
        StateHasChanged();

        try
        {
            var deliveries = await RouteService.GetUnassignedDeliveriesAsync(date.Value);
            _availableDeliveriesCount = deliveries.Count;
            
            Logger.LogInformation($"?? {_availableDeliveriesCount} livraisons disponibles pour le {date:dd/MM/yyyy}");
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"? Erreur lors du comptage des livraisons: {ex.Message}");
            _availableDeliveriesCount = null;
        }
        finally
        {
            _checkingDeliveries = false;
            StateHasChanged();
        }
    }

    #endregion

    #region Google Maps Autocomplete

    private async Task InitializeGoogleMapsAutocompleteAsync()
    {
        try
        {
            Logger.LogInformation("??? Initializing Google Maps autocomplete...");

            var retries = 0;
            const int maxRetries = 10;
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
                        Logger.LogInformation("? Google Maps loaded");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogInformation($"?? Error checking Google Maps: {ex.Message}");
                }
            
                await Task.Delay(100);
                retries++;
            }
        
            if (!isGoogleMapsLoaded)
            {
                Logger.LogInformation("?? Google Maps timeout - autocomplete disabled");
                Snackbar.Add("Autocomplete Google Maps non disponible", Severity.Warning);
                return;
            }
        
            _dotNetRef = DotNetObjectReference.Create(this);
            _googleMapsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                "import", "/js/googleMapsAutocomplete.js");

            await Task.Delay(100);

            await _googleMapsModule.InvokeVoidAsync(
                "initializeAutocomplete",
                _dotNetRef,
                "address-field"
            );
        
            Logger.LogInformation("? Google Maps autocomplete initialized");
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"? Google Maps error: {ex.Message}");
        }
    }

    [JSInvokable]
    public Task OnAddressSelected(GooglePlaceResult place)
    {
        try
        {
            Logger.LogInformation($"?? Address selected: {place.FormattedAddress}");
            
            // ? SAUVEGARDER L'ADRESSE PRÉCÉDENTE
            var previousAddress = State.DepartureAddress;
            var previousLatitude = State.DepartureLatitude;
            var previousLongitude = State.DepartureLongitude;
            
            State.DepartureAddress = place.FormattedAddress ?? string.Empty;
            State.DepartureLatitude = place.Latitude;
            State.DepartureLongitude = place.Longitude;
            
            // ? MARQUER COMME CHANGÉ SI ADRESSE DIFFÉRENTE (en mode édition)
            var addressChanged = !string.IsNullOrEmpty(previousAddress) && 
                                 previousAddress != State.DepartureAddress;
            
            var coordinatesChanged = (previousLatitude.HasValue || previousLongitude.HasValue) &&
                                     (previousLatitude != State.DepartureLatitude || 
                                      previousLongitude != State.DepartureLongitude);
            
            if (addressChanged || coordinatesChanged)
            {
                State.MarkAsChanged();
                Logger.LogInformation($"?? Custom address changed - optimization invalidated");
                Logger.LogInformation($"   Previous: {previousAddress} ({previousLatitude}, {previousLongitude})");
                Logger.LogInformation($"   Current:  {State.DepartureAddress} ({State.DepartureLatitude}, {State.DepartureLongitude})");
            }
            
            _ = OnFieldChanged();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"Error processing address selection: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }

    #endregion

    #region Dispose

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_googleMapsModule != null)
            {
                await _googleMapsModule.InvokeVoidAsync("disposeAutocomplete");
                await _googleMapsModule.DisposeAsync();
            }

            _dotNetRef?.Dispose();
        }
        catch (JSDisconnectedException) { }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error disposing Google Maps module");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Normalise une adresse pour la comparaison en supprimant les caractères spéciaux et espaces multiples
    /// </summary>
    private static string NormalizeAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;

        // Supprimer les caractères spéciaux et normaliser les espaces
        var normalized = address
            .Replace(",", " ")
            .Replace(".", " ")
            .Replace("-", " ")
            .Replace("'", " ")
            .Trim();

        // Réduire les espaces multiples à un seul espace
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized.ToLowerInvariant();
    }

    #endregion

    #region Helper Classes

    private class DepotOption
    {
        public TenantDepotDto? Depot { get; set; }
        public bool IsDefault { get; set; }
        public string DisplayText { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsCustomAddress { get; set; }
    }

    #endregion
}