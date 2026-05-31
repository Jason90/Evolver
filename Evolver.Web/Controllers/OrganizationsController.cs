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
[Route("api/organizations")]
[Authorize]
public sealed class OrganizationsController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet("tree")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Organizations.Query)]
    public async Task<ActionResult<IReadOnlyList<OrganizationTreeNodeDto>>> GetTree(CancellationToken ct)
    {
        var rows = await db.Organizations
            .AsNoTracking()
            .OrderBy(x => x.ParentId)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        var nodes = rows.ToDictionary(
            x => x.Id,
            x => new OrganizationTreeNodeDto(
                x.Id,
                x.ParentId,
                x.Name,
                x.OrgType,
                x.IsActive,
                x.UpdateTime,
                new List<OrganizationTreeNodeDto>()));

        var roots = new List<OrganizationTreeNodeDto>();
        foreach (var n in nodes.Values)
        {
            if (n.ParentId is null || !nodes.TryGetValue(n.ParentId.Value, out var parent))
                roots.Add(n);
            else
                parent.Children.Add(n);
        }

        return Ok(roots);
    }

    [HttpGet("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Organizations.Query)]
    public async Task<ActionResult<OrganizationDto>> GetById(long id, CancellationToken ct)
    {
        var row = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return NotFound();
        return Ok(new OrganizationDto(row.Id, row.ParentId, row.Name, row.OrgType, row.IsActive, row.UpdateTime));
    }

    [HttpPost]
    [RequirePermission(NavSystemSettingsPermissionCodes.Organizations.Create)]
    public async Task<ActionResult<OrganizationDto>> Create([FromBody] CreateOrganizationDto dto, CancellationToken ct)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest("Name is required.");

        if (dto.ParentId is { } pid)
        {
            var parent = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == pid, ct);
            if (parent is null)
                return BadRequest("Parent organization not found.");
        }

        var entity = new Organization
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            ParentId = dto.ParentId,
            Name = name,
            OrgType = string.IsNullOrWhiteSpace(dto.OrgType) ? null : dto.OrgType.Trim(),
            IsActive = dto.IsActive
        };

        db.Organizations.Add(entity);
        await db.SaveChangesAsync(ct);

        return Ok(new OrganizationDto(entity.Id, entity.ParentId, entity.Name, entity.OrgType, entity.IsActive, entity.UpdateTime));
    }

    [HttpPut("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Organizations.Update)]
    public async Task<ActionResult> Update(long id, [FromBody] UpdateOrganizationDto dto, CancellationToken ct)
    {
        var entity = await db.Organizations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound();

        var name = dto.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest("Name is required.");

        if (dto.ParentId is { } newParentId)
        {
            if (newParentId == id)
                return BadRequest("Parent cannot be self.");
            if (!await db.Organizations.AnyAsync(x => x.Id == newParentId, ct))
                return BadRequest("Parent organization not found.");
            if (await IsInSubtreeAsync(id, newParentId, ct))
                return BadRequest("Cannot set parent to a descendant.");
        }

        entity.Name = name;
        entity.ParentId = dto.ParentId;
        entity.OrgType = string.IsNullOrWhiteSpace(dto.OrgType) ? null : dto.OrgType.Trim();
        entity.IsActive = dto.IsActive;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Organizations.Delete)]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await db.Organizations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound();

        if (!entity.IsActive)
            return NoContent();

        entity.IsActive = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<bool> IsInSubtreeAsync(long rootId, long candidateAncestorId, CancellationToken ct)
    {
        var current = candidateAncestorId;
        var guard = 0;
        while (guard++ < 256)
        {
            if (current == rootId)
                return true;
            var parentId = await db.Organizations.AsNoTracking()
                .Where(x => x.Id == current)
                .Select(x => x.ParentId)
                .FirstOrDefaultAsync(ct);
            if (parentId is null)
                return false;
            current = parentId.Value;
        }

        return true;
    }
}
