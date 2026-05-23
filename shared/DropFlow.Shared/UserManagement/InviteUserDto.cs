namespace DropFlow.Shared.UserManagement;

public class InviteUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Manager" ou "Livreur"
}