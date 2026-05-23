namespace DropFlow.Shared.Clients;

public class ClientAddressLookupDto
{
    public int Id { get; set; }
    public string? Label { get; set; }
    public string FullAddress { get; set; } // "123 Rue X, 75001 Paris"
    public bool IsDefault { get; set; }
}