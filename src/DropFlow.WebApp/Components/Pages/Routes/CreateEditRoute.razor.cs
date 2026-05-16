using DropFlow.WebApp.Components.Pages.Routes.Steps;
using DropFlow.WebApp.Models.Routes;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DropFlow.WebApp.Components.Pages.Routes;

public partial class CreateEditRoute
{
    [Parameter] public int? Id { get; set; }
    private string? Reference { get; set; }

    [Inject] private ILogger<CreateEditRoute> Logger { get; set; } = default!;

    private int _activeIndex;
    private readonly RouteWizardState _wizardState = new();
    private bool _loading;
    private bool _submitting;
    private bool IsEditMode => Id.HasValue;

    // Variables de validation par step
    private bool _step1Valid;
    private bool _step2Valid;
    private bool _step3Valid;
    private bool _step4Valid;
    private bool _step5Valid;

    // Messages d'erreur par step
    private string _step1Error = string.Empty;
    private string _step2Error = string.Empty;
    private string _step3Error = string.Empty;
    private string _step4Error = string.Empty;

    // Références aux steps pour validation
    private RouteInfoStep? _step1;
    private RouteTeamStep? _step2;
    private RouteDeliveriesStep? _step3;
    private RouteOptimizeStep? _step4;
    private RouteValidationStep? _step5;

    protected override async Task OnInitializedAsync()
    {
        if (IsEditMode && Id.HasValue)
        {
            await LoadExistingRoute();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Attendre que les composants soient bien rendus
            await Task.Delay(100);
            
            // Notifier le step actif qu'il est affiché
            NotifyStepEntered(_activeIndex);
            
            // Force un rafraîchissement si nécessaire
            StateHasChanged();
        }
    }

    private async Task LoadExistingRoute()
    {
        _loading = true;

        try
        {
            var route = await RouteService.GetRouteByIdAsync(Id!.Value);

            if (route == null)
            {
                Snackbar.Add("Tournée introuvable", Severity.Error);
                NavigationManager.NavigateTo("/tournees");
                return;
            }

            Logger.LogInformation("Loading existing route: {Reference}", route.Reference);

            Reference = route.Reference;
            // ✅ 1. Step 1 - Informations de base
            _wizardState.Date = route.Date;
            _wizardState.VehicleId = route.VehicleId;
            _wizardState.VehicleName = route.VehicleName;
            _wizardState.StartTime = route.StartTime;
            _wizardState.DepartureAddress = route.DepartureAddress ?? string.Empty;
            _wizardState.DepartureLatitude = route.DepartureLatitude;
            _wizardState.DepartureLongitude = route.DepartureLongitude;
            
            Logger.LogInformation("Step 1 loaded - Date: {Date}, Vehicle: {VehicleName}", route.Date.ToString("dd/MM/yyyy"), route.VehicleName);

            // ✅ 2. Step 2 - Équipe
            _wizardState.TeamMembers = route.TeamMembers.Select(tm => new TeamMemberState
            {
                DriverId = tm.DriverId,
                DriverName = tm.DriverName,
                Role = tm.Role
            }).ToList();

            Logger.LogInformation("Step 2 loaded - {TeamMemberCount} team members", route.TeamMembers.Count);

            // ✅ 3. Step 3 - Livraisons (SelectedDeliveries)
            var deliveryIds = route.Deliveries.Select(d => d.DeliveryId).ToList();
            
            if (deliveryIds.Any())
            {
                _wizardState.SelectedDeliveries = route.Deliveries.Select(rd => new DropFlow.Shared.Deliveries.DeliveryDto
                {
                    Id = rd.DeliveryId,
                    Reference = rd.Reference,
                    ClientName = rd.ClientName,
                    Address = rd.Address
                }).ToList();

                Logger.LogInformation("Step 3 loaded - {DeliveryCount} deliveries", deliveryIds.Count);
            }

            // ✅ 4. Step 4 - Optimisation
            _wizardState.OptimizedDeliveries = route.Deliveries
                .OrderBy(d => d.SequenceOrder)
                .Select(rd => new OptimizedDeliveryState
                {
                    DeliveryId = rd.DeliveryId,
                    Reference = rd.Reference,
                    ClientName = rd.ClientName,
                    Address = rd.Address,
                    Latitude = rd.Latitude,
                    Longitude = rd.Longitude,
                    SequenceOrder = rd.SequenceOrder ?? 0,
                    
                    // Optimisation
                    DepartureAddress = rd.DepartureAddress ?? string.Empty,
                    DepartureTime = rd.DepartureTime ?? TimeSpan.Zero,
                    EstimatedArrivalTime = rd.EstimatedArrivalTime ?? TimeSpan.Zero,
                    DurationMinutes = rd.TravelDurationMinutes ?? 0,
                    DistanceToNextMeters = rd.DistanceToNextMeters ?? 0,
                    ServiceDurationMinutes = rd.EstimatedDurationMinutes,
                    
                    TimeSlotStart = rd.EstimatedArrivalTime ?? TimeSpan.Zero,
                    TimeSlotEnd = (rd.EstimatedArrivalTime ?? TimeSpan.Zero).Add(TimeSpan.FromMinutes(rd.EstimatedDurationMinutes))
                }).ToList();

            _wizardState.TotalDistanceKm = route.TotalDistance;
            _wizardState.TotalDurationMinutes = route.TotalDuration;
            _wizardState.TotalServiceDuration = _wizardState.OptimizedDeliveries.Sum(d => d.ServiceDurationMinutes);
            _wizardState.TotalDurationWithService = route.TotalDuration;
            _wizardState.EndTime = route.StartTime.Add(TimeSpan.FromMinutes(route.TotalDuration));

            Logger.LogInformation("Step 4 loaded - Distance: {DistanceKm}km, Duration: {DurationMin}min", route.TotalDistance, route.TotalDuration);

            // ✅ Marquer les steps comme valides
            _step1Valid = true;
            _step2Valid = true;
            _step3Valid = true;
            _step4Valid = true;

            // Marquer comme optimisé pour que les modifications ultérieures déclenchent
            // la réoptimisation mais que la navigation sans changement ne la déclenche pas
            _wizardState.MarkAsOptimized();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading route {RouteId}", Id);
            Snackbar.Add($"Erreur lors du chargement: {ex.Message}", Severity.Error);
            NavigationManager.NavigateTo("/tournees");
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    #region Step Navigation

    private async Task NextStepAsync(dynamic stepper)
    {
        var isValid = await ValidateCurrentStepAsync();
        if (!isValid) return;

        ClearCurrentStepError();
        await stepper.NextStepAsync();
        await Task.Delay(100);
        NotifyStepEntered(_activeIndex);
        StateHasChanged();
    }

    private async Task<bool> ValidateCurrentStepAsync()
    {
        switch (_activeIndex)
        {
            case 0:
                if (_step1 == null) return false;
                var step1Valid = await _step1.ValidateAsync();
                if (!step1Valid) _step1Error = "Veuillez remplir tous les champs obligatoires";
                return step1Valid;

            case 1:
                if (_step2 == null) return false;
                var step2Valid = await _step2.ValidateAsync();
                if (!step2Valid) _step2Error = "Veuillez sélectionner au moins un chauffeur";
                return step2Valid;

            case 2:
                if (_step3 == null) return false;
                var step3Valid = await _step3.ValidateAsync();
                if (!step3Valid) _step3Error = "Veuillez sélectionner au moins une livraison";
                return step3Valid;
            
            case 3:
                if (_step4 == null) return false;
                var step4Valid = await _step4.ValidateAsync();
                if (!step4Valid) _step4Error = "Veuillez optimiser l'itinéraire";
                return step4Valid;

            case 4:
                return true;

            default:
                return false;
        }
    }

    private void ClearCurrentStepError()
    {
        switch (_activeIndex)
        {
            case 0: _step1Error = string.Empty; break;
            case 1: _step2Error = string.Empty; break;
            case 2: _step3Error = string.Empty; break;
            case 3: _step4Error = string.Empty; break;
        }
    }

    private void NotifyStepEntered(int stepIndex)
    {
        switch (stepIndex)
        {
            case 0: _step1?.OnStepEntered(); break;
            case 1: _step2?.OnStepEntered(); break;
            case 2: _step3?.OnStepEntered(); break;
            case 3: _step4?.OnStepEntered(); break;
            case 4: _step5?.OnStepEntered(); break;
        }
    }

    private bool IsCurrentStepValid()
    {
        return _activeIndex switch
        {
            0 => _step1Valid,
            1 => _step2Valid,
            2 => _step3Valid,
            3 => _step4Valid,
            4 => true,
            _ => false
        };
    }

    private bool IsStep5Valid()
    {
        return _step5Valid && 
               _wizardState.IsValidated &&
               _wizardState.OptimizedDeliveries.Any() &&
               _wizardState.TeamMembers.Any();
    }

    #endregion

    #region Validation Handlers

    private void HandleStep1Validation(bool isValid)
    {
        _step1Valid = isValid;
        if (isValid) _step1Error = string.Empty;
        StateHasChanged();
    }

    private void HandleStep2Validation(bool isValid)
    {
        _step2Valid = isValid;
        if (isValid) _step2Error = string.Empty;
        StateHasChanged();
    }

    private void HandleStep3Validation(bool isValid)
    {
        _step3Valid = isValid;
        if (isValid) _step3Error = string.Empty;
        StateHasChanged();
    }

    private void HandleStep4Validation(bool isValid)
    {
        _step4Valid = isValid;
        if (isValid) _step4Error = string.Empty;
        StateHasChanged();
    }

    private void HandleStep5Validation(bool isValid)
    {
        _step5Valid = isValid;
        Logger.LogInformation("Step 5 validation changed: {IsValid}", isValid);
        StateHasChanged();
    }

    #endregion

    #region Submit

    private async Task SubmitRoute()
    {
        _submitting = true;

        try
        {
            if (IsEditMode && Id.HasValue)
            {
                // ✅ MODE EDIT - Mettre à jour la tournée existante avec TOUTES les données
                var updateDto = new DropFlow.Shared.Routes.UpdateRouteDto
                {
                    // ✅ Informations de base
                    Date = _wizardState.Date ?? DateTime.Today,
                    VehicleId = _wizardState.VehicleId!.Value,
                    StartTime = _wizardState.StartTime,
                    DepartureAddress = _wizardState.DepartureAddress,
                    DepartureLatitude = _wizardState.DepartureLatitude,
                    DepartureLongitude = _wizardState.DepartureLongitude,
                    
                    // ✅ Métriques
                    TotalDistance = _wizardState.TotalDistanceKm,
                    TotalDuration = _wizardState.TotalDurationMinutes,
                    
                    // ✅ Équipe
                    Team = _wizardState.TeamMembers.Select(tm => new DropFlow.Shared.Routes.TeamMemberDto
                    {
                        DriverId = tm.DriverId,
                        Role = tm.Role
                    }).ToList(),
                    
                    // ✅ Livraisons avec données d'optimisation
                    Deliveries = _wizardState.OptimizedDeliveries.Select(d => new DropFlow.Shared.Routes.UpdateDeliverySequenceDto
                    {
                        DeliveryId = d.DeliveryId,
                        SequenceOrder = d.SequenceOrder,
                        
                        // ✅ Optimisation - Départ
                        DepartureAddress = d.DepartureAddress,
                        DepartureTime = d.DepartureTime,
                        
                        // ✅ Optimisation - Trajet
                        TravelDurationMinutes = d.DurationMinutes,
                        DistanceToNextMeters = d.DistanceToNextMeters,
                        
                        // ✅ Timing
                        EstimatedArrivalTime = d.EstimatedArrivalTime,
                        TimeSlotStart = d.TimeSlotStart,
                        TimeSlotEnd = d.TimeSlotEnd
                    }).ToList()
                };

                Logger.LogInformation("Updating route {RouteId} with {DeliveryCount} deliveries", Id, updateDto.Deliveries.Count);

                var result = await RouteService.UpdateRouteAsync(Id.Value, updateDto);

                if (result.Succeeded)
                {
                    Logger.LogInformation("Route {RouteId} updated successfully", Id);
                    Snackbar.Add("Tournée modifiée avec succès", Severity.Success);
                    
                    await Task.Delay(500);
                    NavigationManager.NavigateTo("/tournees");
                }
                else
                {
                    var errorMessage = result.Errors.FirstOrDefault() ?? "Erreur lors de la modification";
                    Snackbar.Add(errorMessage, Severity.Error);
                }
            }
            else
            {
                // ✅ MODE CREATE - Créer une nouvelle tournée
                var dto = _wizardState.ToCreateDto();
                
                Logger.LogInformation("Creating new route with {DeliveryCount} deliveries", dto.Deliveries.Count);
                
                var result = await RouteService.CreateRouteAsync(dto);

                if (result.Succeeded)
                {
                    Logger.LogInformation("Route created successfully");
                    Snackbar.Add("Tournée créée avec succès", Severity.Success);
        
                    await Task.Delay(500);
                    NavigationManager.NavigateTo("/tournees");
                }
                else
                {
                    var errorMessage = result.Errors.FirstOrDefault() ?? "Erreur lors de la création";
                    Snackbar.Add(errorMessage, Severity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error submitting route {RouteId}", Id);
            Snackbar.Add($"Erreur: {ex.Message}", Severity.Error);
        }
        finally
        {
            _submitting = false;
        }
    }

    #endregion

    #region Cancel

    private async Task Cancel()
    {
        var message = IsEditMode 
            ? "Voulez-vous vraiment annuler les modifications ?"
            : "Voulez-vous vraiment annuler ? Les données saisies seront perdues.";

        var confirmed = await DialogService.ShowMessageBox(
            IsEditMode ? "Annuler les modifications" : "Annuler la création",
            message,
            yesText: "Oui, annuler",
            cancelText: "Non, continuer");

        if (confirmed == true)
        {
            NavigationManager.NavigateTo("/tournees");
        }
    }

    #endregion
}