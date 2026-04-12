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
    [RequirePermission(NavSystemSettingsPermissionCodes.PermissionsPage.Query)]
    public async Task<ActionResult<IReadOnlyList<PermissionTreeNodeDto>>> GetTree(CancellationToken ct)
    {
        var rows = await db.Permissions
            .AsNoTracking()
            .OrderBy(x => x.ParentId)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(ct);

        var nodes = rows.ToDictionary(
            x => x.Id,
            x => new PermissionTreeNodeDto(
                Id: x.Id,
                ParentId: x.ParentId,
                Type: x.Type.ToString(),
                DisplayType: MapDisplayType(x.Type),
                Code: x.Code,
                Name: x.Name,
                Resource: x.Resource,
                ComponentPath: x.ComponentPath,
                SortOrder: x.SortOrder,
                IsEnabled: x.IsEnabled,
                Icon: x.Icon,
                IsExternalLink: x.IsExternalLink,
                IsVisible: x.IsVisible,
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

        SortTreeChildren(roots);
        return Ok(roots);
    }

    [HttpPost]
    [RequirePermission(NavSystemSettingsPermissionCodes.PermissionsPage.Create)]
    [RequirePermission(NavSystemSettingsPermissionCodes.PermissionsPage.Update)]
    public async Task<ActionResult<PermissionDto>> Upsert([FromBody] UpsertPermissionDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<PermissionType>(dto.Type, ignoreCase: true, out var type))
            return BadRequest("Invalid Type");

        if (type is PermissionType.Menu or PermissionType.Directory)
        {
            if (string.IsNullOrWhiteSpace(dto.Resource) && type == PermissionType.Menu)
                return BadRequest("菜单类型须填写路由地址。");
        }

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
                ComponentPath = dto.ComponentPath?.Trim(),
                SortOrder = dto.SortOrder ?? 0,
                IsEnabled = dto.IsEnabled ?? true,
                Icon = dto.Icon?.Trim(),
                IsExternalLink = dto.IsExternalLink ?? false,
                IsVisible = dto.IsVisible ?? true,
            };
            db.Permissions.Add(existing);
        }
        else
        {
            existing.Name = dto.Name.Trim();
            existing.Type = type;
            existing.ParentId = dto.ParentId;
            existing.Resource = dto.Resource?.Trim();
            existing.ComponentPath = dto.ComponentPath?.Trim();
            if (dto.SortOrder is not null)
                existing.SortOrder = dto.SortOrder.Value;
            if (dto.IsEnabled is not null)
                existing.IsEnabled = dto.IsEnabled.Value;
            if (dto.Icon is not null)
                existing.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? null : dto.Icon.Trim();
            if (dto.IsExternalLink is not null)
                existing.IsExternalLink = dto.IsExternalLink.Value;
            if (dto.IsVisible is not null)
                existing.IsVisible = dto.IsVisible.Value;
        }

        await db.SaveChangesAsync(ct);

        return Ok(new PermissionDto(
            existing.Id,
            existing.ParentId,
            existing.Type.ToString(),
            existing.Code,
            existing.Name,
            existing.Resource,
            existing.ComponentPath,
            existing.SortOrder,
            existing.IsEnabled,
            existing.Icon,
            existing.IsExternalLink,
            existing.IsVisible));
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.PermissionsPage.Delete)]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await db.Permissions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound();

        if (await db.Permissions.AnyAsync(x => x.ParentId == id, ct))
            return BadRequest("存在子权限，请先删除子节点。");

        var links = await db.RolePermissions.Where(x => x.PermissionId == id).ToListAsync(ct);
        db.RolePermissions.RemoveRange(links);
        db.Permissions.Remove(entity);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string MapDisplayType(PermissionType t) =>
        t switch
        {
            PermissionType.Directory => "目录",
            PermissionType.Menu => "菜单",
            PermissionType.UiButton => "按钮",
            PermissionType.Api => "接口",
            _ => t.ToString(),
        };

    private static void SortTreeChildren(List<PermissionTreeNodeDto> level)
    {
        level.Sort((a, b) =>
        {
            var o = a.SortOrder.CompareTo(b.SortOrder);
            return o != 0 ? o : string.Compare(a.Code, b.Code, StringComparison.Ordinal);
        });
        foreach (var n in level)
            SortTreeChildren(n.Children);
    }
}
