using DropFlow.Domain.Common;
using DropFlow.Shared.Enums;

namespace DropFlow.Domain.Entities;

public class Route : ITenantEntity, IAuditableEntity
{
    public int Id { get; private set; }
    public int TenantId { get;  set; }
    
    public string Reference { get;  set; }
    public DateTime Date { get; set; }
    
    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; }
    
    public RouteStatus Status { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan? EstimatedEndTime { get; set; }
    
    // Métriques
    public decimal TotalDistance { get; set; }
    public int TotalDuration { get; set; }
    public int TotalDeliveries { get; set; }
    public int? TotalVolume { get; set; }
    
    // Optimisation
    public string? OptimizedRouteJson { get; set; }
    public string? DepartureAddress { get; set; }
    public double? DepartureLatitude { get; set; }
    public double? DepartureLongitude { get; set; }
    
    /// <summary>
    /// Indique si cette route a été optimisée avec Google Directions API
    /// True = L'ordre vient de l'algorithme Google
    /// False = Ordre défini manuellement ou jamais optimisé
    /// </summary>
    public bool WasOptimizedByGoogle { get; set; }

    /// <summary>
    /// Indique si l'ordre a été modifié manuellement après l'optimisation Google
    /// True = Manager a fait drag & drop après optimisation
    /// False = Ordre intact depuis l'optimisation OU jamais optimisé
    /// </summary>
    public bool WasManuallyReordered { get; set; }
    // Audit
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string ModifiedBy { get; set; }
    
    // Navigation
    public List<RouteTeam> Team { get; set; } = new();
    public List<Delivery> Deliveries { get; set; } = new();
    
    private Route()
    {
        Reference = string.Empty;
        CreatedBy = string.Empty;
        ModifiedBy = string.Empty;
    }
    
    public static Route Create(
        string reference,
        DateTime date,
        int vehicleId,
        TimeSpan startTime,
        string? departureAddress = null,
        double? departureLatitude = null,
        double? departureLongitude = null)
    {
        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Reference is required", nameof(reference));
        
        return new Route
        {
            Reference = reference,
            Date = date.Date,
            VehicleId = vehicleId,
            StartTime = startTime,
            Status = RouteStatus.Draft,
            DepartureAddress = departureAddress,
            DepartureLatitude = departureLatitude,
            DepartureLongitude = departureLongitude,
            TotalDistance = 0,
            TotalDuration = 0,
            TotalDeliveries = 0,
            TotalVolume = 0
        };
    }
    
    public void UpdateMetrics(
        decimal totalDistance,
        int totalDuration,
        int totalDeliveries,
        int totalVolume,
        TimeSpan? estimatedEndTime,
        string? optimizedRouteJson = null)
    {
        TotalDistance = totalDistance;
        TotalDuration = totalDuration;
        TotalDeliveries = totalDeliveries;
        TotalVolume = totalVolume;
        EstimatedEndTime = estimatedEndTime;
        OptimizedRouteJson = optimizedRouteJson;
        ModifiedDate = DateTime.UtcNow;
    }
    
    public void Confirm()
    {
        if (Status != RouteStatus.Draft)
            throw new InvalidOperationException("Can only confirm draft route sheets");
        
        Status = RouteStatus.Confirmed;
        ModifiedDate = DateTime.UtcNow;
    }
    
    public void Start()
    {
        if (Status != RouteStatus.Confirmed)
            throw new InvalidOperationException("Can only start confirmed route sheets");
        
        Status = RouteStatus.InProgress;
        ModifiedDate = DateTime.UtcNow;
    }
    
    public void Complete()
    {
        if (Status != RouteStatus.InProgress)
            throw new InvalidOperationException("Can only complete in-progress route sheets");
        
        Status = RouteStatus.Completed;
        ModifiedDate = DateTime.UtcNow;
    }
    
    public void Cancel()
    {
        if (Status == RouteStatus.Completed)
            throw new InvalidOperationException("Cannot cancel completed route sheets");
        
        Status = RouteStatus.Cancelled;
        ModifiedDate = DateTime.UtcNow;
    }
    // --- ? NOUVELLES MÉTHODES - GESTION OPTIMISATION ---

    /// <summary>
    /// Marquer la route comme optimisée par Google
    /// À appeler après un OptimizeRouteAsync réussi
    /// </summary>
    public void MarkAsOptimizedByGoogle()
    {
        WasOptimizedByGoogle = true;
        WasManuallyReordered = false; // Reset car nouvel ordre optimal
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Marquer la route comme réorganisée manuellement
    /// À appeler après un drag & drop + RecalculateRouteMetricsAsync
    /// </summary>
    public void MarkAsManuallyReordered()
    {
        WasManuallyReordered = true;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Réinitialiser les flags (retour à l'ordre optimal)
    /// À appeler quand le manager clique "Revenir à l'ordre optimal"
    /// </summary>
    public void ResetToOptimalOrder()
    {
        if (!WasOptimizedByGoogle)
            throw new InvalidOperationException("Cannot reset to optimal order - route was never optimized");
    
        WasManuallyReordered = false;
        ModifiedDate = DateTime.UtcNow;
    }
}