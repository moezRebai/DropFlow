using DropFlow.Application.Interfaces;
using DropFlow.Shared.Drivers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DriversController(
    IDriverService driverService,
    IDriverAvailabilityService availabilityService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DriverFilterDto filter)
    {
        var result = await driverService.GetAllAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await driverService.GetByIdAsync(id);
        if (!result.Succeeded)
            return NotFound(result.Errors);
        return Ok(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(string userId)
    {
        var result = await driverService.GetByUserIdAsync(userId);
        if (!result.Succeeded)
            return NotFound(result.Errors);
        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var drivers = await driverService.GetActiveDriversAsync();
        return Ok(drivers);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateDriverDto dto)
    {
        var result = await driverService.CreateAsync(dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDriverDto dto)
    {
        var result = await driverService.UpdateAsync(id, dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await driverService.DeleteAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }

    [HttpGet("{id}/availability")]
    public async Task<IActionResult> CheckAvailability(int id, [FromQuery] DateTime date)
    {
        var result = await availabilityService.CheckAvailabilityAsync(id, date);
        return Ok(result);
    }

    [HttpPost("availability/check")]
    public async Task<IActionResult> CheckMultipleAvailability(
        [FromBody] List<int> driverIds, 
        [FromQuery] DateTime date)
    {
        var results = await availabilityService.CheckMultipleAvailabilityAsync(driverIds, date);
        return Ok(results);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable([FromQuery] DateTime date)
    {
        var drivers = await availabilityService.GetAvailableDriversAsync(date);
        return Ok(drivers);
    }
}