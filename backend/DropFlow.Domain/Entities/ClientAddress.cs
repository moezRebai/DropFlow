namespace DropFlow.Domain.Entities;

public class ClientAddress
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

    public string FullAddress =>
        string.IsNullOrWhiteSpace(Complement)
            ? $"{Address}, {ZipCode} {City}"
            : $"{Address}, {Complement}, {ZipCode} {City}";
    
    // Navigation
    public Client Client { get; set; }
    public List<Delivery> Deliveries { get; set; } = new();
}