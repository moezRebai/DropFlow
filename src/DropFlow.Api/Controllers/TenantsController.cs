using DropFlow.Application.Interfaces;
using DropFlow.Shared.Tenants;
using DropFlow.Shared.Tenants.Depots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantsController(ITenantManagementService tenantService) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════
    // TENANT INFO
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Récupère les informations du tenant courant
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentTenant()
    {
        var result = await tenantService.GetCurrentTenantAsync();
        if (!result.Succeeded)
            return NotFound(result.Errors);
        return Ok(result.Data);
    }
    
    /// <summary>
    /// Met à jour les informations générales de l'entreprise
    /// </summary>
    [HttpPut("company-info")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateCompanyInfo([FromBody] UpdateTenantCompanyInfoDto dto)
    {
        var result = await tenantService.UpdateCompanyInfoAsync(dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }
    
    /// <summary>
    /// Met à jour les informations légales
    /// </summary>
    [HttpPut("legal-info")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateLegalInfo([FromBody] UpdateTenantLegalInfoDto dto)
    {
        var result = await tenantService.UpdateLegalInfoAsync(dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }
    
    /// <summary>
    /// Met à jour le logo de l'entreprise
    /// </summary>
    [HttpPut("logo")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateLogo([FromBody] UpdateTenantLogoDto dto)
    {
        var result = await tenantService.UpdateLogoAsync(dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }
    
    /// <summary>
    /// Supprime le logo de l'entreprise
    /// </summary>
    [HttpDelete("logo")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RemoveLogo()
    {
        var result = await tenantService.RemoveLogoAsync();
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }
    
    // ═══════════════════════════════════════════════════════════
    // DEPOTS
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Récupère tous les dépôts actifs (pour dropdowns)
    /// </summary>
    [HttpGet("depots/all")]
    public async Task<IActionResult> GetAllDepots()
    {
        var result = await tenantService.GetAllDepotsAsync();
        return Ok(result);
    }
    
    /// <summary>
    /// Récupère les dépôts avec pagination et filtres
    /// </summary>
    [HttpGet("depots")]
    public async Task<IActionResult> GetDepots([FromQuery] TenantDepotFilterDto filter)
    {
        var result = await tenantService.GetDepotsAsync(filter);
        return Ok(result);
    }
    
    /// <summary>
    /// Récupère un dépôt par son ID
    /// </summary>
    [HttpGet("depots/{id}")]
    public async Task<IActionResult> GetDepotById(int id)
    {
        var result = await tenantService.GetDepotByIdAsync(id);
        if (!result.Succeeded)
            return NotFound(result.Errors);
        return Ok(result.Data);
    }
    
    /// <summary>
    /// Crée un nouveau dépôt
    /// </summary>
    [HttpPost("depots")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateDepot([FromBody] CreateTenantDepotDto dto)
    {
        var result = await tenantService.CreateDepotAsync(dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return CreatedAtAction(nameof(GetDepotById), new { id = result.Data }, result.Data);
    }
    
    /// <summary>
    /// Met à jour un dépôt existant
    /// </summary>
    [HttpPut("depots/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateDepot(int id, [FromBody] UpdateTenantDepotDto dto)
    {
        var result = await tenantService.UpdateDepotAsync(id, dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }
    
    /// <summary>
    /// Supprime un dépôt
    /// </summary>
    [HttpDelete("depots/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDepot(int id)
    {
        var result = await tenantService.DeleteDepotAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }
    
    /// <summary>
    /// Définit un dépôt comme par défaut
    /// </summary>
    [HttpPost("depots/{id}/set-default")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SetDefaultDepot(int id)
    {
        var result = await tenantService.SetDefaultDepotAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }
    
    /// <summary>
    /// Active/Désactive un dépôt
    /// </summary>
    [HttpPost("depots/{id}/toggle-status")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ToggleDepotStatus(int id)
    {
        var result = await tenantService.ToggleDepotStatusAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }
}