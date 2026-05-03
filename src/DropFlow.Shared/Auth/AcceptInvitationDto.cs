namespace DropFlow.Shared.Auth;

public record AcceptInvitationDto
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}