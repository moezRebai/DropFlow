namespace DropFlow.Shared.Clients;

/// <summary>
/// DTO pour mettre à jour une adresse client existante
/// </summary>
public class UpdateClientAddressDto
{
    public string? Label { get; set; }
    public string Address { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Complement { get; set; }
}