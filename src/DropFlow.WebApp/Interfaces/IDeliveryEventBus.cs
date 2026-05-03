namespace DropFlow.WebApp.Interfaces;

/// <summary>
/// Event Bus pour notifier les changements de livraisons entre composants Blazor
/// Implémenté en Singleton pour partager les événements entre tous les circuits/onglets
/// </summary>
public interface IDeliveryEventBus
{
    /// <summary>
    /// Événement déclenché quand une livraison est mise à jour
    /// </summary>
    event Action<int>? OnDeliveryUpdated;
    
    /// <summary>
    /// Événement déclenché quand une livraison est créée
    /// </summary>
    event Action<int>? OnDeliveryCreated;
    
    /// <summary>
    /// Événement déclenché quand une livraison est supprimée
    /// </summary>
    event Action<int>? OnDeliveryDeleted;
    
    /// <summary>
    /// Déclenche l'événement de mise à jour d'une livraison
    /// </summary>
    void TriggerDeliveryUpdated(int deliveryId);
    
    /// <summary>
    /// Déclenche l'événement de création d'une livraison
    /// </summary>
    void TriggerDeliveryCreated(int deliveryId);
    
    /// <summary>
    /// Déclenche l'événement de suppression d'une livraison
    /// </summary>
    void TriggerDeliveryDeleted(int deliveryId);
}
