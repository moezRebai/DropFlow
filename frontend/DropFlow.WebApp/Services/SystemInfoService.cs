using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DropFlow.WebApp.Services;

public class DbInfoDto
{
    public string Label  { get; set; } = "—";
    public string Host   { get; set; } = "—";
    public bool   IsNeon { get; set; }
}

public class SystemInfoService(
    IHttpClientFactory httpClientFactory,
    ProtectedLocalStorage localStorage,
    ILogger<SystemInfoService> logger)
    : BaseApiService(httpClientFactory, localStorage, logger)
{
    public Task<DbInfoDto?> GetDbInfoAsync()
        => GetAsync<DbInfoDto>("/api/system/db-info");
}
