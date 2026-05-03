namespace DropFlow.Shared.Auth;

public class ResetPasswordDto
{
    public string Email { get; set; }
    public string Token { get; set; }
    public int TenantId { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmNewPassword { get; set; }
}
