namespace DropFlow.Shared.Deliveries;

public abstract class DeliveryBaseDto
{
    // Client
    public int? ClientId { get; set; }
    public int? ClientAddressId { get; set; }
    public string ClientFirstName { get; set; }
    public string ClientLastName { get; set; }
    public string ClientPhone { get; set; }
    public string ClientEmail { get; set; }
    public string? AddressLabel { get; set; }
    public string Address { get; set; }
    public string ZipCode { get; set; }
    public string City { get; set; }
    public string? AddressComplement { get; set; }
}