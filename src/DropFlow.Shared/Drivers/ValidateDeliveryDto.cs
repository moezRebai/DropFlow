namespace DropFlow.Shared.Drivers;

/// <summary>
/// Payload envoyé par l'app mobile pour valider une livraison
/// POST /api/driver/deliveries/{id}/validate
/// </summary>
public class ValidateDeliveryDto
{
    /// <summary>
    /// Signature client en base64 (PNG)
    /// Obligatoire sauf si IsClientAbsent = true
    /// </summary>
    public string? SignatureBase64 { get; set; }
    
    /// <summary>
    /// Photo preuve de livraison en base64 (JPEG)
    /// Optionnel
    /// </summary>
    public string? PhotoBase64 { get; set; }
    
    /// <summary>
    /// Commentaire libre du livreur
    /// </summary>
    public string? Comment { get; set; }
    
    /// <summary>
    /// Client absent au moment de la livraison
    /// Si true, la signature n'est pas obligatoire
    /// </summary>
    public bool IsClientAbsent { get; set; }
}
