using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Evolver.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public sealed class PermissionsController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet("tree")]
    [RequirePermission("permissions.read")]
    public async Task<ActionResult<IReadOnlyList<PermissionTreeNodeDto>>> GetTree(CancellationToken ct)
    {
        var rows = await db.Permissions
            .AsNoTracking()
            .OrderBy(x => x.ParentId)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        var nodes = rows.ToDictionary(
            x => x.Id,
            x => new PermissionTreeNodeDto(
                Id: x.Id,
                ParentId: x.ParentId,
                Type: x.Type.ToString(),
                Code: x.Code,
                Name: x.Name,
                Resource: x.Resource,
                Children: new List<PermissionTreeNodeDto>()
            )
        );

        var roots = new List<PermissionTreeNodeDto>();
        foreach (var n in nodes.Values)
        {
            if (n.ParentId is null || !nodes.TryGetValue(n.ParentId.Value, out var parent))
                roots.Add(n);
            else
                parent.Children.Add(n);
        }

        return Ok(roots);
    }

    [HttpPost]
    [RequirePermission("permissions.write")]
    public async Task<ActionResult<PermissionDto>> Upsert([FromBody] UpsertPermissionDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<PermissionType>(dto.Type, ignoreCase: true, out var type))
            return BadRequest("Invalid Type");

        var existing = await db.Permissions.FirstOrDefaultAsync(x => x.Code == dto.Code, ct);
        if (existing is null)
        {
            existing = new Permission
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                Type = type,
                ParentId = dto.ParentId,
                Resource = dto.Resource?.Trim(),
            };
            db.Permissions.Add(existing);
        }
        else
        {
            existing.Name = dto.Name.Trim();
            existing.Type = type;
            existing.ParentId = dto.ParentId;
            existing.Resource = dto.Resource?.Trim();
        }

        await db.SaveChangesAsync(ct);

        return Ok(new PermissionDto(existing.Id, existing.ParentId, existing.Type.ToString(), existing.Code, existing.Name, existing.Resource));
    }
}
