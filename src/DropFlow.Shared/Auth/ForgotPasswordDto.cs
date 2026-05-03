namespace DropFlow.Shared.Auth;

public class ForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;

    // Parameterless ctor (model binding / JSON deserialization)
    public ForgotPasswordDto()
    {
    }

    // Convenience ctor
    public ForgotPasswordDto(string email)
    {
        Email = email;
    }
}
