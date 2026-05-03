namespace DropFlow.Shared.Auth;

public record UserTenantInfoDto(
    int TenantId,
    string TenantName,
    string Role,
    bool IsActive
);