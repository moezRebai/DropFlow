using DropFlow.Application.Interfaces.Drivers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.Api.Controllers;

/// <summary>
/// Sert les fichiers uploadés (signatures, photos de livraison)
/// Les fichiers sont stockés sur le disque du serveur Windows
/// 
/// URL : GET /api/files/{tenantId}/deliveries/{deliveryId}/{filename}
/// Exemple : GET /api/files/1/deliveries/42/signature_20260208_143022.png
/// </summary>
[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController(IFileStorageService fileStorageService) : ControllerBase
{
    /// <summary>
    /// Récupère un fichier uploadé par son chemin relatif
    /// Accessible par le livreur (sa livraison) et le manager/admin (toutes)
    /// </summary>
    [HttpGet("{*relativePath}")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetFile(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return BadRequest();
        
        var bytes = await fileStorageService.GetFileAsync(relativePath);
        
        if (bytes == null)
            return NotFound();
        
        // Déterminer le content type
        var contentType = relativePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) 
            ? "image/png" 
            : relativePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
              relativePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                ? "image/jpeg"
                : "application/octet-stream";
        
        return File(bytes, contentType);
    }
}
