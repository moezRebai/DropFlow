namespace DropFlow.Shared.Clients;

public class ClientLookupDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; }
    public string Phone { get; set; }
    public string? Email { get; set; }
    public List<ClientAddressLookupDto> Addresses { get; set; }
}