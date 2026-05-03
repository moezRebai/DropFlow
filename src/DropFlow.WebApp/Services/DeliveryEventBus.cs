using DropFlow.WebApp.Interfaces;

namespace DropFlow.WebApp.Services;

/// <summary>
/// Event Bus Singleton pour propager les événements de livraison
/// entre tous les composants Blazor (même à travers différents onglets/circuits)
/// </summary>
public class DeliveryEventBus(ILogger<DeliveryEventBus> logger) : IDeliveryEventBus
{
    // ════════════════════════════════════════════════════════════════
    // EVENTS
    // ════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Événement déclenché quand une livraison est mise à jour
    /// </summary>
    public event Action<int>? OnDeliveryUpdated;
    
    /// <summary>
    /// Événement déclenché quand une livraison est créée
    /// </summary>
    public event Action<int>? OnDeliveryCreated;
    
    /// <summary>
    /// Événement déclenché quand une livraison est supprimée
    /// </summary>
    public event Action<int>? OnDeliveryDeleted;
    
    // ════════════════════════════════════════════════════════════════
    // TRIGGER METHODS
    // ════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Déclenche l'événement de mise à jour d'une livraison
    /// Tous les composants abonnés seront notifiés
    /// </summary>
    public void TriggerDeliveryUpdated(int deliveryId)
    {
        logger.LogInformation("🔔 Event Bus: Triggering DeliveryUpdated for delivery {DeliveryId}", deliveryId);
        
        // Vérifier s'il y a des abonnés
        var subscriberCount = OnDeliveryUpdated?.GetInvocationList().Length ?? 0;
        logger.LogInformation("   📡 Broadcasting to {Count} subscriber(s)", subscriberCount);
        
        // Déclencher l'événement
        OnDeliveryUpdated?.Invoke(deliveryId);
    }
    
    /// <summary>
    /// Déclenche l'événement de création d'une livraison
    /// </summary>
    public void TriggerDeliveryCreated(int deliveryId)
    {
        logger.LogInformation("🔔 Event Bus: Triggering DeliveryCreated for delivery {DeliveryId}", deliveryId);
        
        var subscriberCount = OnDeliveryCreated?.GetInvocationList().Length ?? 0;
        logger.LogInformation("   📡 Broadcasting to {Count} subscriber(s)", subscriberCount);
        
        OnDeliveryCreated?.Invoke(deliveryId);
    }
    
    /// <summary>
    /// Déclenche l'événement de suppression d'une livraison
    /// </summary>
    public void TriggerDeliveryDeleted(int deliveryId)
    {
        logger.LogInformation("🔔 Event Bus: Triggering DeliveryDeleted for delivery {DeliveryId}", deliveryId);
        
        var subscriberCount = OnDeliveryDeleted?.GetInvocationList().Length ?? 0;
        logger.LogInformation("   📡 Broadcasting to {Count} subscriber(s)", subscriberCount);
        
        OnDeliveryDeleted?.Invoke(deliveryId);
    }
}
