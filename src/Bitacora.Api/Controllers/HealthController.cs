using Bitacora.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Bitacora.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController(ApplicationDbContext database) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        var connected = await database.Database.CanConnectAsync(timeout.Token);

        return StatusCode(
            connected ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable,
            new { status = connected ? "healthy" : "unhealthy", server = "up", database = connected ? "up" : "down" });
    }
}
