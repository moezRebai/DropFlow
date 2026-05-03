namespace DropFlow.Shared.Clients;

/// <summary>
/// DTO pour filtrer la liste des clients avec pagination
/// </summary>
public class ClientFilterDto
{
    public string? SearchTerm { get; set; } // Nom, Email, Téléphone
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}