namespace DropFlow.Shared.Admin;

public class TenantDetailsDto(List<TenantUserDto> recentUsers) : TenantDto
{
    public List<TenantUserDto> RecentUsers { get; } = recentUsers;
}
