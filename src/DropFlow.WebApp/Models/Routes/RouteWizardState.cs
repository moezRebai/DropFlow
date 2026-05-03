using DropFlow.Shared.Deliveries;
using DropFlow.Shared.Routes;

namespace DropFlow.WebApp.Models.Routes;

public class RouteWizardState
{
    // ════════════════════════════════════════════════════════════════
    // DÉTECTION DE CHANGEMENTS (pour invalidation optimisation)
    // ════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Indique si les données ont changé depuis la dernière optimisation
    /// Si true, une réoptimisation est nécessaire
    /// </summary>
    public bool HasChanged { get; set; }
    
    /// <summary>
    /// Hash des livraisons lors de la dernière optimisation
    /// Utilisé pour détecter les changements (adresse, durée, etc.)
    /// </summary>
    private string? _lastDeliveriesHash;
    
    /// <summary>
    /// Hash du point de départ lors de la dernière optimisation
    /// Utilisé pour détecter les changements de dépôt/adresse
    /// </summary>
    private string? _lastDepartureHash;
    
    // ════════════════════════════════════════════════════════════════
    // STEP 1 - INFORMATIONS DE BASE
    // ════════════════════════════════════════════════════════════════
    
    public DateTime? Date { get; set; } = DateTime.Today;
    public int? VehicleId { get; set; }
    public string? VehicleName { get; set; }
    public TimeSpan StartTime { get; set; } = new(8, 0, 0);
    public string DepartureAddress { get; set; } = string.Empty;
    public double? DepartureLatitude { get; set; }
    public double? DepartureLongitude { get; set; }
    public int? DepotId { get; set; }
    
    public bool WasOptimizedByGoogle { get; set; }
    public bool IsManuallyReordered { get; set; }
    // ════════════════════════════════════════════════════════════════
    // STEP 2 - ÉQUIPE
    // ════════════════════════════════════════════════════════════════
    
    public List<TeamMemberState> TeamMembers { get; set; } = new();
    
    // ════════════════════════════════════════════════════════════════
    // STEP 3 - LIVRAISONS SÉLECTIONNÉES
    // ════════════════════════════════════════════════════════════════
    
    public List<DeliveryDto> SelectedDeliveries { get; set; } = new();
    
    // ════════════════════════════════════════════════════════════════
    // STEP 4 - OPTIMISATION
    // ════════════════════════════════════════════════════════════════
    
    public List<OptimizedDeliveryState> OptimizedDeliveries { get; set; } = new();
    public decimal TotalDistanceKm { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int TotalServiceDuration { get; set; }
    public int TotalDurationWithService { get; set; }
    public TimeSpan EndTime { get; set; }
    
    // ════════════════════════════════════════════════════════════════
    // STEP 5 - VALIDATION
    // ════════════════════════════════════════════════════════════════
    
    public bool IsValidated { get; set; }
    
    // ════════════════════════════════════════════════════════════════
    // MÉTHODES DE DÉTECTION DE CHANGEMENTS
    // ════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Calcule un hash des livraisons sélectionnées pour détecter les modifications
    /// Inclut : ID, Adresse, Durée, Coordonnées GPS
    /// </summary>
    public string GetDeliveriesHash()
    {
        if (SelectedDeliveries == null || !SelectedDeliveries.Any())
            return string.Empty;
        
        // Créer une chaîne avec les données importantes de chaque livraison
        var data = string.Join("|", SelectedDeliveries
            .OrderBy(d => d.Id)
            .Select(d => $"{d.Id}:{d.Address}:{d.EstimatedDurationMinutes}:{d.Latitude}:{d.Longitude}"));
        
        // Retourner le hash code
        return data.GetHashCode().ToString();
    }
    
    /// <summary>
    /// Calcule un hash du point de départ pour détecter les changements
    /// Inclut : Adresse, Latitude, Longitude
    /// </summary>
    public string GetDepartureHash()
    {
        if (string.IsNullOrEmpty(DepartureAddress))
            return string.Empty;
        
        return $"{DepartureAddress}:{DepartureLatitude}:{DepartureLongitude}".GetHashCode().ToString();
    }
    
    /// <summary>
    /// Vérifie si les livraisons ont changé depuis la dernière optimisation
    /// Retourne true si des modifications sont détectées
    /// </summary>
    public bool HaveDeliveriesChanged()
    {
        var currentHash = GetDeliveriesHash();
        
        // Première fois, sauvegarder le hash sans signaler de changement
        if (_lastDeliveriesHash == null)
        {
            _lastDeliveriesHash = currentHash;
            return false;
        }
        
        // Comparer avec le hash sauvegardé
        if (_lastDeliveriesHash != currentHash)
        {
            Console.WriteLine($"🔄 Deliveries hash changed:");
            Console.WriteLine($"   Previous: {_lastDeliveriesHash}");
            Console.WriteLine($"   Current:  {currentHash}");
            HasChanged = true;
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Vérifie si le point de départ a changé depuis la dernière optimisation
    /// </summary>
    public bool HasDepartureChanged()
    {
        var currentHash = GetDepartureHash();
        
        // Première fois, sauvegarder le hash sans signaler de changement
        if (_lastDepartureHash == null)
        {
            _lastDepartureHash = currentHash;
            return false;
        }
        
        // Comparer avec le hash sauvegardé
        if (_lastDepartureHash != currentHash)
        {
            Console.WriteLine($"🔄 Departure hash changed:");
            Console.WriteLine($"   Previous: {_lastDepartureHash}");
            Console.WriteLine($"   Current:  {currentHash}");
            HasChanged = true;
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Marque l'état comme modifié et invalide l'optimisation existante
    /// À appeler quand on change : dépôt, véhicule, livraisons, etc.
    /// </summary>
    public void MarkAsChanged()
    {
        HasChanged = true;
        
        // Vider les données d'optimisation devenues obsolètes
        OptimizedDeliveries.Clear();
        TotalDistanceKm = 0;
        TotalDurationMinutes = 0;
        TotalServiceDuration = 0;
        TotalDurationWithService = 0;
        EndTime = StartTime;
        
        Console.WriteLine("⚠️ WizardState marked as changed - optimization invalidated");
    }
    
    /// <summary>
    /// Marque l'état comme optimisé et sauvegarde les hash actuels
    /// À appeler après une optimisation réussie
    /// </summary>
    public void MarkAsOptimized()
    {
        HasChanged = false;
        
        // Sauvegarder les hash pour future comparaison
        _lastDeliveriesHash = GetDeliveriesHash();
        _lastDepartureHash = GetDepartureHash();
        
        Console.WriteLine("✅ WizardState marked as optimized");
        Console.WriteLine($"   Deliveries hash: {_lastDeliveriesHash}");
        Console.WriteLine($"   Departure hash: {_lastDepartureHash}");
    }
    
    // ════════════════════════════════════════════════════════════════
    // VALIDATION
    // ════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Vérifie si toutes les données nécessaires sont présentes pour créer la tournée
    /// </summary>
    public bool IsComplete()
    {
        return Date.HasValue &&
               VehicleId.HasValue &&
               !string.IsNullOrEmpty(DepartureAddress) &&
               TeamMembers.Any() &&
               OptimizedDeliveries.Any() &&
               !HasChanged; // ✅ L'optimisation doit être à jour
    }
    
    // ════════════════════════════════════════════════════════════════
    // CONVERSION EN DTO
    // ════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Convertit l'état du wizard en DTO pour création de tournée
    /// </summary>
    public CreateRouteDto ToCreateDto()
    {
        return new CreateRouteDto
        {
            Date = Date!.Value,
            VehicleId = VehicleId!.Value,
            StartTime = StartTime,
            DepartureAddress = DepartureAddress,
            DepartureLatitude = DepartureLatitude,
            DepartureLongitude = DepartureLongitude,
            
            TotalDistance = TotalDistanceKm,
            TotalDuration = TotalDurationMinutes,
            
            Team = TeamMembers.Select(tm => new TeamMemberDto
            {
                DriverId = tm.DriverId,
                Role = tm.Role
            }).ToList(),
            
            Deliveries = OptimizedDeliveries.Select(d => new CreateDeliverySequenceDto
            {
                DeliveryId = d.DeliveryId,
                SequenceOrder = d.SequenceOrder,
                
                // Données d'optimisation
                DepartureAddress = d.DepartureAddress,
                DepartureTime = d.DepartureTime,
                TravelDurationMinutes = d.DurationMinutes,
                DistanceToNextMeters = d.DistanceToNextMeters,
                EstimatedArrivalTime = d.EstimatedArrivalTime,
                TimeSlotStart = d.TimeSlotStart,
                TimeSlotEnd = d.TimeSlotEnd
            }).ToList()
        };
    }
}