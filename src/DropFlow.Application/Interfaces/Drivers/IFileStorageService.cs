namespace DropFlow.Application.Interfaces.Drivers;

/// <summary>
/// Service de stockage fichiers sur le serveur Windows
/// Sauvegarde signatures et photos dans D:\DropFlow\uploads\{tenantId}\deliveries\{deliveryId}\
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Sauvegarde un fichier base64 sur le disque
    /// </summary>
    /// <param name="tenantId">ID du tenant pour l'isolation</param>
    /// <param name="deliveryId">ID de la livraison</param>
    /// <param name="base64Content">Contenu du fichier en base64</param>
    /// <param name="fileType">Type : "signature" ou "photo"</param>
    /// <param name="extension">Extension : "png" ou "jpg"</param>
    /// <returns>Chemin relatif du fichier sauvegardé (stocké en DB)</returns>
    Task<string> SaveFileAsync(int tenantId, int deliveryId, string base64Content, string fileType, string extension);
    
    /// <summary>
    /// Récupère le contenu d'un fichier depuis son chemin relatif
    /// </summary>
    Task<byte[]?> GetFileAsync(string relativePath);
    
    /// <summary>
    /// Supprime un fichier
    /// </summary>
    Task<bool> DeleteFileAsync(string relativePath);
}
