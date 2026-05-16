namespace DropFlow.Shared.Auth;

public record AuthResult(
    bool Success,
    string? Token = null,
    string? RefreshToken = null,
    string? Message = null,
    UserDto? User = null);