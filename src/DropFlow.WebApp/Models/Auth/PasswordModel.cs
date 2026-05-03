namespace DropFlow.WebApp.Models.Auth;

public class PasswordModel
{
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}