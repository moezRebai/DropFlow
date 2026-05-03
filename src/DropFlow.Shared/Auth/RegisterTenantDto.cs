namespace DropFlow.Shared.Auth;

public record RegisterTenantDto(
    string CompanyName,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword);