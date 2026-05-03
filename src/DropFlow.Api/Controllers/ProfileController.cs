using DropFlow.Application.Interfaces.Users;
using DropFlow.Shared.Profil;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController(IProfileService profileService)
    : ControllerBase
{
    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await profileService.GetCurrentUserProfileAsync();
        if (profile == null)
            return NotFound(new { message = "Profile not found" });

        return Ok(profile);
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    [HttpPut]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var result = await profileService.UpdateProfileAsync(dto);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Change password
    /// </summary>
    [HttpPut("password")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var result = await profileService.ChangePasswordAsync(dto);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Get user preferences
    /// </summary>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(UserPreferencesDto), 200)]
    public async Task<IActionResult> GetPreferences()
    {
        var preferences = await profileService.GetPreferencesAsync();
        return Ok(preferences);
    }

    /// <summary>
    /// Update user preferences
    /// </summary>
    [HttpPut("preferences")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UserPreferencesDto dto)
    {
        var result = await profileService.UpdatePreferencesAsync(dto);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }
}