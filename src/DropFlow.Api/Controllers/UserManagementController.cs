using System.Security.Claims;
using DropFlow.Application.Interfaces.Users;
using DropFlow.Shared.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserManagementController(IUserManagementService userService) : ControllerBase
{
    [HttpPost("invite")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var result = await userService.InviteUserAsync(dto, userId);

        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpPost("{userId}/deactivate")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> DeactivateUser(string userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var result = await userService.DeactivateUserAsync(userId, currentUserId);

        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpPost("{userId}/activate")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> ReactivateUser(string userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
        var result = await userService.ReactivateUserAsync(userId, currentUserId);

        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpGet("users")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> GetTenantUsers(
        [FromQuery] bool includeDeactivated = false,
        [FromQuery] bool includeDeleted = false)
    {
        var tenantId = int.Parse(User.FindFirst("TenantId")?.Value!);
        
        var users = await userService.GetTenantUsersAsync(tenantId,
        includeDeactivated, 
        includeDeleted);
        
        return Ok(users);
    }
    
    /// <summary>
    /// Change user role (Manager and Admin only)
    /// </summary>
    [HttpPut("users/{userId}/role")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> ChangeUserRole(
        string userId,
        [FromBody] ChangeUserRoleDto request)
    {
        var result = await userService.ChangeUserRoleAsync(userId, request.NewRole);
    
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }
    
    /// <summary>
    /// Delete user (soft delete - archives the user)
    /// </summary>
    [HttpDelete("users/{userId}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var result = await userService.DeleteUserAsync(userId);
    
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }
    
    /// <summary>
    /// Restore a deleted user
    /// </summary>
    [HttpPost("users/{userId}/restore")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RestoreUser(string userId)
    {
        var result = await userService.RestoreUserAsync(userId);
    
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }
}