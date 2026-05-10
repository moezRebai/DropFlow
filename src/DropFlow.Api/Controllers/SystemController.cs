using DropFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DropFlow.Api.Controllers;

[ApiController]
[Route("api/system")]
[Authorize]
public class SystemController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("db-info")]
    public IActionResult GetDbInfo()
    {
        var cs = db.Database.GetConnectionString() ?? string.Empty;

        var host = cs.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().Split('=', 2))
            .Where(p => p.Length == 2 && p[0].Equals("Host", StringComparison.OrdinalIgnoreCase))
            .Select(p => p[1])
            .FirstOrDefault() ?? "localhost";

        var isNeon = host.Contains("neon.tech", StringComparison.OrdinalIgnoreCase);

        return Ok(new
        {
            label = isNeon ? "Neon" : "Local",
            host  = isNeon ? "Neon Cloud" : host,
            isNeon
        });
    }
}
