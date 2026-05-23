namespace DropFlow.Shared.Clients;

public class ClientDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DisplayName => $"{FirstName} {LastName}";
    public string Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public List<ClientAddressDto> Addresses { get; set; }
    public int TotalDeliveries { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime CreatedDate { get; set; }
}