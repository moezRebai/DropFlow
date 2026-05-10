using DropFlow.Application.Interfaces.Deliveries;
using DropFlow.Shared.Deliveries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeliveriesController(IDeliveryService deliveryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDeliveries([FromQuery] DeliveryFilterDto filter)
    {
        var result = await deliveryService.GetDeliveriesAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDeliveryById(int id)
    {
        var result = await deliveryService.GetDeliveryByIdAsync(id);
        if (!result.Succeeded)
            return NotFound(result.Errors);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDelivery([FromBody] CreateDeliveryDto dto)
    {
        var result = await deliveryService.CreateDeliveryAsync(dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return CreatedAtAction(nameof(GetDeliveryById), new { id = result.Data }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDelivery(int id, [FromBody] UpdateDeliveryDto dto)
    {
        var result = await deliveryService.UpdateDeliveryAsync(id, dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteDelivery(int id)
    {
        var result = await deliveryService.DeleteDeliveryAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDeliveryRequest request)
    {
        var result = await deliveryService.UpdateStatusAsync(id, request.Status);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }
    
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await deliveryService.GetStatsAsync();
        return Ok(stats);
    }
    
    [HttpPost("{id}/duplicate")]
    public async Task<IActionResult> DuplicateDelivery(int id)
    {
        var result = await deliveryService.DuplicateDeliveryAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }

    /// <summary>
    /// Changement de statut en masse
    /// </summary>
    [HttpPost("batch/status")]
    public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateStatusRequest request)
    {
        var result = await deliveryService.BulkUpdateStatusAsync(request.DeliveryIds, request.Status);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }

    /// <summary>
    /// Suppression en masse
    /// </summary>
    [HttpPost("batch/delete")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
    {
        var result = await deliveryService.BulkDeleteAsync(request.DeliveryIds);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }
    
    [HttpGet("unassigned")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetUnassignedDeliveries([FromQuery] DateTime date)
    {
        var result = await deliveryService.GetUnassignedDeliveriesAsync(date);
       
        if (!result.Succeeded)
            return NotFound(result.Errors);
        
        return Ok(result);
    }
    
    /// <summary>
    /// ✅ NOUVEAU - Récupère les livraisons disponibles pour ajout à une tournée
    /// Exclut automatiquement les livraisons verrouillées dans des tournées actives (Confirmed, InProgress)
    /// Inclut les livraisons de la tournée courante si currentRouteId est fourni (mode édition)
    /// </summary>
    /// <param name="date">Date de la tournée</param>
    /// <param name="currentRouteId">ID de la tournée courante (optionnel, pour mode édition)</param>
    /// <returns>Liste des livraisons disponibles avec indication si déjà dans une route Draft</returns>
    [HttpPost("{id}/geocode")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GeocodeDelivery(int id)
    {
        var result = await deliveryService.GeocodeDeliveryAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }

    [HttpGet("available-for-route")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAvailableForRoute(
        [FromQuery] DateTime date,
        [FromQuery] int? currentRouteId = null)
    {
        var result = await deliveryService.GetAvailableDeliveriesForRouteAsync(date, currentRouteId);
        
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        
        return Ok(result);
    }
}