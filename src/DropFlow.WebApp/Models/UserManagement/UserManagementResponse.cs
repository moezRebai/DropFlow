namespace DropFlow.WebApp.Models.UserManagement;

/// <summary>
/// Response générique pour les opérations UserManagement
/// </summary>
public class UserManagementResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}