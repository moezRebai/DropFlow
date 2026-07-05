namespace DropFlow.Shared.Clients;

public class ClientAddressLookupDto
{
    public int Id { get; set; }
    public string? Label { get; set; }
    public string FullAddress { get; set; }
    public string Address { get; set; }
    public string ZipCode { get; set; }
    public string City { get; set; }
    public bool IsDefault { get; set; }
}