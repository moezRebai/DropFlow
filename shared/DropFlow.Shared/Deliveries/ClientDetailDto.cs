namespace DropFlow.Shared.Deliveries;

/// <summary>
/// Détails client pour pré-remplissage formulaire
/// </summary>
public class ClientDetailDto
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}