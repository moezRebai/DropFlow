using DropFlow.Application.Interfaces.Drivers;
using DropFlow.Shared.Drivers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.Api.Controllers;

/// <summary>
/// Endpoints dédiés à l'application mobile livreur
/// 
/// Tous les endpoints nécessitent une authentification JWT.
/// Le livreur ne voit que ses propres livraisons (via ses routes assignées).
/// Les rôles Manager et Admin peuvent aussi accéder (utile pour debug/test).
/// </summary>
[ApiController]
[Route("api/driver")]
[Authorize]
public class DriverAppController(IDriverAppService driverAppService) : ControllerBase
{
    /// <summary>
    /// Récupère la tournée du jour du livreur connecté
    /// Retourne la route Confirmed ou InProgress assignée au livreur pour aujourd'hui
    /// </summary>
    /// <returns>
    /// HasRoute=true + Route si une tournée existe
    /// HasRoute=false + Message explicatif sinon
    /// </returns>
    [HttpGet("route/today")]
    [ProducesResponseType(typeof(DriverTodayResponse), 200)]
    public async Task<IActionResult> GetTodayRoute()
    {
        var result = await driverAppService.GetTodayRouteAsync();
        return Ok(result);
    }

    /// <summary>
    /// Récupère le détail d'une livraison (vue livreur)
    /// Vérifie automatiquement que la livraison appartient à une route du livreur
    /// 
    /// Données exclues (sécurité) :
    /// - InternalNotes (notes confidentielles manager)
    /// - Price (prix prestation)
    /// - StorePaymentAmount (commission magasin)
    /// </summary>
    [HttpGet("deliveries/{id:int}")]
    [ProducesResponseType(typeof(DriverDeliveryDetailDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetDeliveryDetail(int id)
    {
        var result = await driverAppService.GetDeliveryDetailAsync(id);
        
        if (!result.Succeeded)
        {
            if (result.Errors?.FirstOrDefault()?.Contains("non autorisé") == true)
                return Forbid();
            
            return NotFound(new { message = result.Errors?.FirstOrDefault() });
        }
        
        return Ok(result.Data);
    }

    /// <summary>
    /// Valide une livraison avec signature, photo et commentaire
    /// 
    /// Règles :
    /// - Signature obligatoire sauf si IsClientAbsent=true
    /// - Photo optionnelle
    /// - Passe le statut à Delivered
    /// - Sauvegarde les fichiers sur le serveur
    /// </summary>
    [HttpPost("deliveries/{id:int}/validate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ValidateDelivery(int id, [FromBody] ValidateDeliveryDto dto)
    {
        var result = await driverAppService.ValidateDeliveryAsync(id, dto);
        
        if (!result.Succeeded)
        {
            if (result.Errors?.FirstOrDefault()?.Contains("non autorisé") == true)
                return Forbid();
            
            return BadRequest(new { message = result.Errors?.FirstOrDefault() });
        }
        
        return Ok(new { message = "Livraison validée avec succès" });
    }

    /// <summary>
    /// Démarre la tournée du livreur
    /// - Route : Confirmed → InProgress
    /// - Livraisons Confirmed → InProgress
    /// </summary>
    [HttpPost("route/{id:int}/start")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> StartRoute(int id)
    {
        var result = await driverAppService.StartRouteAsync(id);
        
        if (!result.Succeeded)
            return BadRequest(new { message = result.Errors?.FirstOrDefault() });
        
        return Ok(new { message = "Tournée démarrée" });
    }

    /// <summary>
    /// Termine la tournée du livreur
    /// - Route : InProgress → Completed
    /// </summary>
    [HttpPost("route/{id:int}/complete")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CompleteRoute(int id)
    {
        var result = await driverAppService.CompleteRouteAsync(id);
        
        if (!result.Succeeded)
            return BadRequest(new { message = result.Errors?.FirstOrDefault() });
        
        return Ok(new { message = "Tournée terminée" });
    }
}
