using DropFlow.Application.Interfaces;
using DropFlow.Application.Interfaces.Drivers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DropFlow.Infrastructure.Services;

/// <summary>
/// Stockage fichiers sur le système de fichiers Windows Server
/// 
/// Structure :
/// {BasePath}\{tenantId}\deliveries\{deliveryId}\signature_20260208_143022.png
/// {BasePath}\{tenantId}\deliveries\{deliveryId}\photo_20260208_143025.jpg
/// 
/// Configuration appsettings.json :
/// "FileStorage": {
///     "BasePath": "D:\\DropFlow\\uploads"
/// }
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
    {
        _logger = logger;
        _basePath = configuration["FileStorage:BasePath"] 
                    ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads");
        
        // S'assurer que le dossier racine existe
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
    }

    public async Task<string> SaveFileAsync(
        int tenantId, 
        int deliveryId, 
        string base64Content, 
        string fileType, 
        string extension)
    {
        try
        {
            // Nettoyer le base64 (retirer le préfixe data:image/xxx;base64, si présent)
            var cleanBase64 = CleanBase64(base64Content);
            var bytes = Convert.FromBase64String(cleanBase64);
            
            // Vérifier la taille (max 5 MB)
            if (bytes.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 5MB limit");
            
            // Construire le chemin
            var directory = Path.Combine(
                _basePath,
                tenantId.ToString(),
                "deliveries",
                deliveryId.ToString());
            
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            // Nom de fichier : type_yyyyMMdd_HHmmss.ext
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{fileType}_{timestamp}.{extension}";
            var fullPath = Path.Combine(directory, fileName);
            
            // Sauvegarder
            await File.WriteAllBytesAsync(fullPath, bytes);
            
            // Retourner le chemin relatif (stocké en DB)
            var relativePath = Path.Combine(
                tenantId.ToString(),
                "deliveries",
                deliveryId.ToString(),
                fileName);
            
            _logger.LogInformation(
                "File saved: {FileType} for delivery {DeliveryId} tenant {TenantId} → {Path}",
                fileType, deliveryId, tenantId, relativePath);
            
            return relativePath;
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid base64 content for {FileType} delivery {DeliveryId}", fileType, deliveryId);
            throw new ArgumentException("Invalid base64 content", ex);
        }
    }

    public async Task<byte[]?> GetFileAsync(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));

        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
            return null;

        if (!File.Exists(fullPath))
            return null;

        return await File.ReadAllBytesAsync(fullPath);
    }

    public Task<bool> DeleteFileAsync(string relativePath)
    {
        var fullPath = Path.Combine(_basePath, relativePath);
        
        if (!File.Exists(fullPath))
            return Task.FromResult(false);
        
        File.Delete(fullPath);
        
        _logger.LogInformation("File deleted: {Path}", relativePath);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Nettoie le préfixe base64 si présent
    /// "data:image/png;base64,iVBOR..." → "iVBOR..."
    /// </summary>
    private static string CleanBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            throw new ArgumentException("Base64 content is empty");
        
        var commaIndex = base64.IndexOf(',');
        return commaIndex >= 0 ? base64[(commaIndex + 1)..] : base64;
    }
}
