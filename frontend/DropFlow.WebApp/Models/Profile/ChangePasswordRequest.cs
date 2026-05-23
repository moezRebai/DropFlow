namespace DropFlow.WebApp.Models.Profile;

/// <summary>
/// Request pour PUT /api/profile/password
/// </summary>
public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}