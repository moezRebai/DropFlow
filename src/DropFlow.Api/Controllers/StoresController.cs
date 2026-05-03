using DropFlow.Application.Interfaces;
using DropFlow.Shared.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StoresController(IStoreService storeService) : ControllerBase
{
    [HttpGet("all")]
    public async Task<IActionResult> GetAllStores()
    {
        var result = await storeService.GetAllStoresAsync();
        return Ok(result);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetStores([FromQuery] StoreFilterDto filter)
    {
        var result = await storeService.GetStoresAsync(filter);
        return Ok(result);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> GetStoresLookup()
    {
        var result = await storeService.GetStoresLookupAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStoreById(int id)
    {
        var result = await storeService.GetStoreByIdAsync(id);
        if (!result.Succeeded)
            return NotFound(result.Errors);
        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreDto dto)
    {
        var result = await storeService.CreateStoreAsync(dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return CreatedAtAction(nameof(GetStoreById), new { id = result.Data }, result.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateStore(int id, [FromBody] UpdateStoreDto dto)
    {
        var result = await storeService.UpdateStoreAsync(id, dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteStore(int id)
    {
        var result = await storeService.DeleteStoreAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }
}