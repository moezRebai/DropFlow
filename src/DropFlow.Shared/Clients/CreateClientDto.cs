namespace DropFlow.Shared.Clients;

public class CreateClientDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    
    // Première adresse (obligatoire)
    public CreateClientAddressDto Address { get; set; }
}