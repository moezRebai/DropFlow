using DropFlow.Application.Interfaces;
using DropFlow.Shared.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DropFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController(IClientService clientService) : ControllerBase
{
    /// <summary>
    /// Recherche de clients (autocomplete pour formulaire livraison)
    /// </summary>
    [HttpGet("search")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SearchClients([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Ok(new List<ClientLookupDto>());
        }

        var result = await clientService.SearchClientsAsync(query);
        return Ok(result);
    }
   
    /// <summary>
    /// Récupérer la liste complète des clients avec pagination et filtres
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetClients([FromQuery] ClientFilterDto filter)
    {
        var result = await clientService.GetClientsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Récupérer l'historique des livraisons d'un client
    /// </summary>
    [HttpGet("{id}/deliveries")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetClientDeliveries(int id)
    {
        var deliveries = await clientService.GetClientDeliveriesAsync(id);
        return Ok(deliveries);
    }

    /// <summary>
    /// Récupérer un client par ID avec ses adresses
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetClientById(int id)
    {
        var result = await clientService.GetClientByIdAsync(id);
        if (!result.Succeeded)
            return NotFound(result.Errors);
        return Ok(result.Data);
    }

    /// <summary>
    /// Récupérer les adresses d'un client
    /// </summary>
    [HttpGet("{id}/addresses")]
    public async Task<IActionResult> GetClientAddresses(int id)
    {
        var result = await clientService.GetClientAddressesAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Créer un nouveau client avec adresse
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientDto dto)
    {
        var result = await clientService.CreateClientAsync(dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return CreatedAtAction(nameof(GetClientById), new { id = result.Data }, result.Data);
    }

    /// <summary>
    /// Ajouter une nouvelle adresse à un client
    /// </summary>
    [HttpPost("{id}/addresses")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> AddAddress(int id, [FromBody] CreateClientAddressDto dto)
    {
        var result = await clientService.AddAddressAsync(id, dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result.Data);
    }
    
    /// <summary>
    /// Modifier un client existant
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateClient(int id, [FromBody] UpdateClientDto dto)
    {
        var result = await clientService.UpdateClientAsync(id, dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }

    /// <summary>
    /// Mettre à jour une adresse client existante
    /// </summary>
    [HttpPut("{clientId}/addresses/{addressId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateAddress(int clientId, int addressId, [FromBody] UpdateClientAddressDto dto)
    {
        var result = await clientService.UpdateAddressAsync(clientId, addressId, dto);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }
    
    /// <summary>
    /// Supprimer un client (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        var result = await clientService.DeleteClientAsync(id);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }

    /// <summary>
    /// Définir une adresse comme adresse par défaut
    /// </summary>
    [HttpPut("{clientId}/addresses/{addressId}/set-default")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SetDefaultAddress(int clientId, int addressId)
    {
        var result = await clientService.SetDefaultAddressAsync(clientId, addressId);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }

    /// <summary>
    /// Supprimer une adresse d'un client
    /// </summary>
    [HttpDelete("{clientId}/addresses/{addressId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteAddress(int clientId, int addressId)
    {
        var result = await clientService.DeleteAddressAsync(clientId, addressId);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return NoContent();
    }
}