using DropFlow.Domain.Common;
using DropFlow.Shared.Enums;

namespace DropFlow.Domain.Entities;

public class Delivery : ITenantEntity, IAuditableEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int SequentialNumber { get; set; }
    public string Reference { get; set; }
    
    // --- FOREIGN KEYS ---
    
    public int ClientId { get; set; }
    public Client Client { get; set; }
    public int ClientAddressId { get; set; }
    public ClientAddress ClientAddress { get; set; }
    
    public int StoreId { get; set; }
    public Store Store { get; set; }
    
    // --- DETAILS ---
    public string FileNumber { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public decimal Price { get; set; }
    public decimal? ClientPaymentAmount { get; set; }
    public decimal? StorePaymentAmount { get; set; }
    
    // --- TYPE & ORGANIZATION ---
    
    public DeliveryType Type { get; set; }
    public DeliveryStatus Status { get; set; }
    
    // Route Sheet (Standard deliveries)
    public int? RouteId { get; set; }  
    public Route Route { get; set; }
    public int? SequenceOrder { get; set; } // Renommé depuis RouteOrder pour cohérence
    
    // --- ? NOUVEAUX CHAMPS - OPTIMISATION ROUTE ---
    
    /// <summary>
    /// Adresse de départ pour cette livraison dans la tournée
    /// (soit le dépôt, soit l'adresse de la livraison précédente)
    /// </summary>
    public string? DepartureAddress { get; set; }
    
    /// <summary>
    /// Heure de départ depuis le point précédent
    /// </summary>
    public TimeSpan? DepartureTime { get; set; }
    
    /// <summary>
    /// Durée du trajet VERS cette livraison (en minutes)
    /// Calculé par Google Maps API
    /// </summary>
    public int? TravelDurationMinutes { get; set; }
    
    /// <summary>
    /// Distance VERS cette livraison (en mètres)
    /// Calculé par Google Maps API
    /// </summary>
    public int? DistanceToNextMeters { get; set; }
    
    // --- END NOUVEAUX CHAMPS ---
    
    // Urgent Driver (Urgent deliveries)
    public int? UrgentDriverId { get; set; }  
    public Driver UrgentDriver { get; set; }
    public bool WithAssembly { get; set; }
    public string? DeliveryNotes { get; set; }
    public string? InternalNotes { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public int? TimeSlotId { get; set; }
    public virtual TimeSlot? TimeSlot { get; set; }
    
    // --- TIMING ---
    
    public TimeSpan? EstimatedArrivalTime { get; set; }  
    public TimeSpan? ActualArrivalTime { get; set; }  
    
    // --- ITEMS ---
    
    public List<DeliveryItem> Items { get; set; } = new();
    public int TotalPackages => Items?.Sum(i => (int)i.Quantity) ?? 0;
    
    // --- PROOF OF DELIVERY ---
    
    public string? SignatureUrl { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime? DeliveredDateTime { get; set; }
    
    /// <summary>
    /// Commentaire libre du livreur lors de la validation
    /// Renseigné depuis l'app mobile
    /// </summary>
    public string? ValidationComment { get; set; }
    
    /// <summary>
    /// ID du Driver qui a validé la livraison
    /// FK vers Drivers (nullable)
    /// </summary>
    public int? ValidatedByDriverId { get; set; }
    public Driver? ValidatedByDriver { get; set; }
    
    /// <summary>
    /// Indique si le client était absent lors de la livraison
    /// Si true, la signature n'est pas obligatoire
    /// </summary>
    public bool IsClientAbsent { get; set; }
    // --- AUDIT ---
    
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string ModifiedBy { get; set; }
    
    // --- MÉTHODES DE MISE À JOUR ---
    
    /// <summary>
    /// Mettre à jour les informations d'optimisation quand la livraison est ajoutée à une route
    /// </summary>
    public void UpdateRouteOptimization(
        int routeId,
        int sequenceOrder,
        string departureAddress,
        TimeSpan departureTime,
        TimeSpan estimatedArrivalTime,
        int travelDurationMinutes,
        int distanceToNextMeters)
    {
        RouteId = routeId;
        SequenceOrder = sequenceOrder;
        DepartureAddress = departureAddress;
        DepartureTime = departureTime;
        EstimatedArrivalTime = estimatedArrivalTime;
        TravelDurationMinutes = travelDurationMinutes;
        DistanceToNextMeters = distanceToNextMeters;
    }
    
    /// <summary>
    /// Réinitialiser les informations d'optimisation (quand on retire de la route)
    /// </summary>
    public void ClearRouteOptimization()
    {
        RouteId = null;
        SequenceOrder = null;
        DepartureAddress = null;
        DepartureTime = null;
        EstimatedArrivalTime = null;
        TravelDurationMinutes = null;
        DistanceToNextMeters = null;
    }
}