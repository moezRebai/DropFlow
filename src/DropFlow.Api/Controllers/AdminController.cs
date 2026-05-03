using DropFlow.Application.Interfaces.Users;
using DropFlow.Domain.Constants;
using DropFlow.Shared.Admin;
using DropFlow.Shared.Profil;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
public class AdminController(
    IAdminService adminService)
    : ControllerBase
{
    // ════════════════════════════════════════════════════════════════
    // TENANTS
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get all tenants (Admin only)
    /// </summary>
    [HttpGet("tenants")]
    [ProducesResponseType(typeof(List<TenantDto>), 200)]
    public async Task<IActionResult> GetAllTenants()
    {
        var tenants = await adminService.GetAllTenantsAsync();
        return Ok(tenants);
    }

    /// <summary>
    /// Get tenant details (Admin only)
    /// </summary>
    [HttpGet("tenants/{tenantId}")]
    [ProducesResponseType(typeof(TenantDetailsDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTenantDetails(int tenantId)
    {
        var tenant = await adminService.GetTenantDetailsAsync(tenantId);
        if (tenant == null)
            return NotFound(new { message = "Tenant not found" });

        return Ok(tenant);
    }

    /// <summary>
    /// Activate a tenant (Admin only)
    /// </summary>
    [HttpPost("tenants/{tenantId}/activate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ActivateTenant(int tenantId)
    {
        var result = await adminService.ActivateTenantAsync(tenantId);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Deactivate a tenant (Admin only)
    /// </summary>
    [HttpPost("tenants/{tenantId}/deactivate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> DeactivateTenant(int tenantId)
    {
        var result = await adminService.DeactivateTenantAsync(tenantId);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Update tenant plan (Admin only)
    /// </summary>
    [HttpPut("tenants/{tenantId}/plan")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateTenantPlan(
        int tenantId,
        [FromBody] UpdateTenantPlanDto dto)
    {
        var result = await adminService.UpdateTenantPlanAsync(tenantId, dto);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Delete a tenant (soft delete) (Admin only)
    /// </summary>
    [HttpDelete("tenants/{tenantId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> DeleteTenant(int tenantId)
    {
        var result = await adminService.DeleteTenantAsync(tenantId);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    // ════════════════════════════════════════════════════════════════
    // USERS
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get all users of a tenant (Admin only)
    /// </summary>
    [HttpGet("tenants/{tenantId}/users")]
    [ProducesResponseType(typeof(List<TenantUserDto>), 200)]
    public async Task<IActionResult> GetTenantUsers(int tenantId)
    {
        var users = await adminService.GetTenantUsersAsync(tenantId);
        return Ok(users);
    }

    /// <summary>
    /// Activate a user (Admin only)
    /// </summary>
    [HttpPost("tenants/{tenantId}/users/{userId}/activate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ActivateUser(int tenantId, string userId)
    {
        var result = await adminService.ActivateUserAsync(tenantId, userId);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Deactivate a user (Admin only)
    /// </summary>
    [HttpPost("tenants/{tenantId}/users/{userId}/deactivate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> DeactivateUser(int tenantId, string userId)
    {
        var result = await adminService.DeactivateUserAsync(tenantId, userId);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    // ════════════════════════════════════════════════════════════════
    // STATS & AUDIT
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get global platform statistics (Admin only)
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(GlobalStatsDto), 200)]
    public async Task<IActionResult> GetGlobalStats()
    {
        var stats = await adminService.GetGlobalStatsAsync();
        return Ok(stats);
    }

    [HttpGet("audit")]
    [ProducesResponseType(typeof(List<AuditLogDto>), 200)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int? tenantId = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? severity = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var logs = await adminService.GetAuditLogsAsync(
            tenantId,
            userId,
            action,
            severity,
            startDate,
            endDate,
            pageNumber,
            Math.Min(pageSize, 200));
    
        return Ok(logs);
    }
    
    /// <summary>
    /// Get all users across all tenants (paginated with filters)
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(UserProfileDto), 200)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int? tenantId = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool includeDeactivated = false, 
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await adminService.GetAllUsersAsync(
            tenantId,
            role,
            isActive,
            searchTerm,
            includeDeactivated,
            includeDeleted,
            pageNumber,
            Math.Min(pageSize, 200));
    
        return Ok(result);
    }

    /// <summary>
    /// Get global user statistics
    /// </summary>
    [HttpGet("users/stats")]
    [ProducesResponseType(typeof(UserStatsDto), 200)]
    public async Task<IActionResult> GetUserStats()
    {
        var stats = await adminService.GetUserStatsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Activate user globally (Admin only)
    /// </summary>
    [HttpPost("users/{userId}/activate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ActivateUserGlobal(string userId)
    {
        var result = await adminService.ActivateUserGlobalAsync(userId);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Deactivate user globally (Admin only)
    /// </summary>
    [HttpPost("users/{userId}/deactivate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> DeactivateUserGlobal(string userId)
    {
        var result = await adminService.DeactivateUserGlobalAsync(userId);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }
}