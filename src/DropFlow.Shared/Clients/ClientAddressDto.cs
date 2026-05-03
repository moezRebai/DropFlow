namespace DropFlow.Shared.Clients;

public class ClientAddressDto
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    
    public string? Label { get; set; }
    public string Address { get; set; }
    public string ZipCode { get; set; }
    public string City { get; set; }
    public string? Complement { get; set; }
    
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    public bool IsDefault { get; set; }
    
    // Propriété calculée pour affichage
    public string FullAddress => $"{Address}, {ZipCode} {City}";
}