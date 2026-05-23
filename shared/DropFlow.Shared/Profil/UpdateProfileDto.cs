namespace DropFlow.Shared.Profil;

public record UpdateProfileDto(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Address
);