namespace DropFlow.Shared.Auth;

public record LoginDto(
    string Email,
    string Password,
    int TenantId);