using Evolver.Application.MenuIntelligence;
using Evolver.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/menu-intelligence")]
[Authorize]
public sealed class MenuIntelligenceController(MenuIntelligenceService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MenuIntelligenceDto>>> Get([FromQuery] string? market, CancellationToken ct)
    {
        var rows = await svc.GetLatestAsync(market, ct);
        return Ok(rows);
    }
}
