using DropFlow.Application.Interfaces;
using DropFlow.Shared.Routes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoutesController(IRouteService routeService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAll([FromQuery] RouteFilterDto filter)
    {
        var result = await routeService.GetAllAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await routeService.GetByIdAsync(id);
        if (!result.Succeeded)
            return NotFound(result.Errors);
        return Ok(result);
    }

    [HttpPost("optimize")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> OptimizeRoute([FromBody] OptimizePathRequestDto request)
    {
        var result = await routeService.OptimizeRouteAsync(request);
        return Ok(result);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateRouteDto dto)
    {
        var result = await routeService.CreateAsync(dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRouteDto dto)
    {
        var result = await routeService.UpdateAsync(id, dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await routeService.DeleteAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }

    [HttpPost("{id}/teamMember")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> AddTeamMember(int id, [FromBody] TeamMemberDto dto)
    {
        var result = await routeService.AddTeamMemberAsync(id, dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }

    [HttpDelete("{id}/team/{driverId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RemoveTeamMember(int id, int driverId)
    {
        var result = await routeService.RemoveTeamMemberAsync(id, driverId);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }

    [HttpPost("{id}/deliveries/{deliveryId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> AddDelivery(int id, int deliveryId)
    {
        var result = await routeService.AddDeliveryAsync(id, deliveryId);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }

    [HttpDelete("{id}/deliveries/{deliveryId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RemoveDelivery(int id, int deliveryId)
    {
        var result = await routeService.RemoveDeliveryAsync(id, deliveryId);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }

    [HttpPut("{id}/sequence")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateSequence(int id, [FromBody] List<UpdateDeliverySequenceDto> sequences)
    {
        var result = await routeService.UpdateSequenceAsync(id, sequences);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }

    [HttpPost("{id}/confirm")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Confirm(int id)
    {
        var result = await routeService.ConfirmAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> Start(int id)
    {
        var result = await routeService.StartAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }

    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await routeService.CompleteAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await routeService.CancelAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result);
    }

    [HttpPost("{id}/recalculate")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RecalculateMetrics(int id)
    {
        // var result = await routeService.RecalculateMetricsAsync(id);
        // if (!result.Succeeded)
        //     return BadRequest(result.Errors);
        return Ok();
    }
    
    /// <summary>
    /// Recalcule les métriques en gardant l'ordre (optimize:false)
    /// Utilisé après un drag & drop manuel dans le wizard
    /// </summary>
    [HttpPost("recalculate-path")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RecalculatePathMetrics([FromBody] OptimizePathRequestDto request)
    {
        var result = await routeService.RecalculateRouteMetricsAsync(request);
    
        if (!result.Succeeded)
            return BadRequest(result.Errors);
    
        return Ok(result);
    }
    
    [HttpGet("{id}/download-sheet")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DownloadRouteSheet(int id)
    {
        var result = await routeService.GenerateRouteSheetPdfAsync(id);
    
        if (!result.Succeeded)
            return BadRequest(new { error = result.Errors?.FirstOrDefault() ?? "Erreur lors de la génération du PDF" });
    
        var fileName = $"Feuille-Route-{id}-{DateTime.Now:yyyyMMdd-HHmmss}.pdf";
    
        return File(result.Data, "application/pdf", fileName);
    }
}