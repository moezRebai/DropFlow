using DropFlow.Application.Interfaces;
using DropFlow.Shared.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VehiclesController(IVehicleService vehicleService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] VehicleFilterDto filter)
    {
        var result = await vehicleService.GetAllAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await vehicleService.GetByIdAsync(id);
        if (!result.Succeeded)
            return NotFound(result.Errors);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
    {
        var result = await vehicleService.CreateAsync(dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVehicleDto dto)
    {
        var result = await vehicleService.UpdateAsync(id, dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin, Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await vehicleService.DeleteAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }

    [HttpGet("{id}/availability")]
    public async Task<IActionResult> CheckAvailability(int id, [FromQuery] DateTime date)
    {
        var result = await vehicleService.IsAvailableAsync(id, date);
        return Ok(result);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable([FromQuery] DateTime date)
    {
        var vehicles = await vehicleService.GetAvailableVehiclesAsync(date);
        return Ok(vehicles);
    }
}