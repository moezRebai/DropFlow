using DropFlow.Application.Interfaces;
using DropFlow.Shared.TimeSlots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimeSlotsController(ITimeSlotService timeSlotService) : ControllerBase
{
    /// <summary>
    /// Récupère tous les créneaux
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAll()
    {
        var timeSlots = await timeSlotService.GetAllAsync();
        return Ok(timeSlots);
    }

    /// <summary>
    /// Récupère un créneau par ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var timeSlot = await timeSlotService.GetByIdAsync(id);
        if (timeSlot == null)
            return NotFound();

        return Ok(timeSlot);
    }

    /// <summary>
    /// Crée un nouveau créneau
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateTimeSlotDto dto)
    {
        var result = await timeSlotService.CreateAsync(dto);
        if (!result.Succeeded)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data }, result);
    }

    /// <summary>
    /// Met à jour un créneau
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTimeSlotDto dto)
    {
        var result = await timeSlotService.UpdateAsync(id, dto);
        if (!result.Succeeded)
            return BadRequest(result);

        return NoContent();
    }

    /// <summary>
    /// Supprime un créneau
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await timeSlotService.DeleteAsync(id);
        if (!result.Succeeded)
            return BadRequest(result);

        return NoContent();
    }
}