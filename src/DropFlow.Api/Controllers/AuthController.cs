using DropFlow.Application.Interfaces.Users;
using DropFlow.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DropFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterTenantDto dto)
    {
        var result = await authService.RegisterTenantAsync(dto);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await authService.LoginAsync(dto);

        if (!result.Success)
            return Unauthorized(new { message = result.Message });

        return Ok(result);
    }

    [HttpPost("accept-invitation")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationDto dto)
    {
        var result = await authService.AcceptInvitationAsync(dto);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result);
    }
    
    /// <summary>
    /// Request password reset (sends email with reset token)
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var result = await authService.ForgotPasswordAsync(dto);
    
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new PasswordResetResponse{ Message = result.Message, Success = result.Succeeded});
    }

    /// <summary>
    /// Reset password with token from email
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var result = await authService.ResetPasswordAsync(dto);
    
        if (!result.Succeeded)
            return BadRequest(new { message = result.Message });

        return Ok(new PasswordResetResponse{ Message = result.Message, Success = result.Succeeded});
    }
    
    [HttpGet("tenants")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(List<UserTenantInfoDto>), 200)]
    public async Task<IActionResult> GetUserTenants([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required" });
    
        var tenants = await authService.GetUserTenantsAsync(email);
        return Ok(tenants.Select(t => new { t.TenantId, t.TenantName }));
    }
}