using System.Collections.Generic;
using Evolver.Core.Entities;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Evolver.Web.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Evolver.Web.Services;

public sealed class TenantProvisioningService(
    AppDbContext db,
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    ILookupNormalizer nameNormalizer,
    IOptions<PlatformOptions> platformOptions)
{
    private readonly PlatformOptions _platform = platformOptions.Value;

    public async Task<ProvisionTenantResponseDto> ProvisionAsync(ProvisionTenantRequestDto dto, CancellationToken ct)
    {
        var tenantName = dto.TenantName.Trim();
        var adminName = dto.AdminUserName.Trim();
        if (string.IsNullOrEmpty(tenantName))
            throw new ArgumentException("租户名称不能为空。", nameof(dto));
        if (string.IsNullOrEmpty(adminName) || string.IsNullOrEmpty(dto.AdminPassword))
            throw new ArgumentException("管理员用户名与密码不能为空。", nameof(dto));

        var rootOrgName = string.IsNullOrWhiteSpace(dto.RootOrgName) ? "总部" : dto.RootOrgName.Trim();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var existsName = await db.Tenants.IgnoreQueryFilters()
            .AnyAsync(t => t.Name == tenantName && !t.IsDeleted, ct);
        if (existsName)
            throw new InvalidOperationException($"租户名称「{tenantName}」已存在。");

        var maxId = await db.Tenants.IgnoreQueryFilters().MaxAsync(t => (int?)t.Id, ct) ?? 0;
        var newTenantId = maxId + 1;

        db.Tenants.Add(new Tenant
        {
            Id = newTenantId,
            TenantId = newTenantId,
            OrgId = 0,
            Name = tenantName,
            IsDeleted = false
        });
        await db.SaveChangesAsync(ct);

        var rootOrg = new Organization
        {
            TenantId = newTenantId,
            OrgId = 1,
            ParentId = null,
            Name = rootOrgName,
            OrgType = "Headquarters"
        };
        db.Organizations.Add(rootOrg);
        await db.SaveChangesAsync(ct);

        var templatePerms = await db.Permissions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.TenantId == _platform.PlatformTenantId && p.OrgId == _platform.TemplateOrgId)
            .ToListAsync(ct);

        if (templatePerms.Count == 0)
            throw new InvalidOperationException(
                $"模板租户 {_platform.PlatformTenantId} 下无权限数据，请先完成平台权限初始化。");

        var oldToNewPermId = new Dictionary<long, long>();
        var remaining = templatePerms.ToList();
        while (remaining.Count > 0)
        {
            var batch = remaining
                .Where(p => p.ParentId is null || oldToNewPermId.ContainsKey(p.ParentId.Value))
                .ToList();
            if (batch.Count == 0)
                throw new InvalidOperationException("权限树数据异常，无法复制。");

            foreach (var old in batch)
            {
                var perm = new Permission
                {
                    TenantId = newTenantId,
                    OrgId = 1,
                    Code = old.Code,
                    Name = old.Name,
                    Type = old.Type,
                    Resource = old.Resource,
                    ParentId = old.ParentId is null ? null : oldToNewPermId[old.ParentId.Value]
                };
                db.Permissions.Add(perm);
                await db.SaveChangesAsync(ct);
                oldToNewPermId[old.Id] = perm.Id;
                remaining.Remove(old);
            }
        }

        var templateAdmin = await roleManager.Roles.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.TenantId == _platform.PlatformTenantId && r.Name == "Admin",
                ct);
        if (templateAdmin is null)
            throw new InvalidOperationException($"模板租户 {_platform.PlatformTenantId} 缺少 Admin 角色。");

        var tplLinks = await db.RolePermissions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(rp =>
                rp.TenantId == _platform.PlatformTenantId &&
                rp.OrgId == _platform.TemplateOrgId &&
                rp.RoleId == templateAdmin.Id)
            .ToListAsync(ct);

        // 不用 RoleManager.CreateAsync：内部仍会走基于 NormalizedName 的查找，易与多租户数据冲突；
        // 直接插入并设置 NormalizedName / ConcurrencyStamp（与 EF RoleStore 一致）。
        var newAdminRole = new AppRole
        {
            Name = "Admin",
            NormalizedName = nameNormalizer.NormalizeName("Admin"),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            TenantId = newTenantId,
            OrgId = 1
        };
        db.Set<AppRole>().Add(newAdminRole);
        await db.SaveChangesAsync(ct);

        foreach (var link in tplLinks)
        {
            if (!oldToNewPermId.TryGetValue(link.PermissionId, out var newPid))
                continue;
            db.RolePermissions.Add(new RolePermission
            {
                TenantId = newTenantId,
                OrgId = 1,
                RoleId = newAdminRole.Id,
                PermissionId = newPid
            });
        }

        await db.SaveChangesAsync(ct);

        var adminUser = new AppUser
        {
            UserName = adminName,
            Email = string.IsNullOrWhiteSpace(dto.AdminEmail) ? null : dto.AdminEmail.Trim(),
            EmailConfirmed = true,
            TenantId = newTenantId,
            OrgId = 1,
            IsActive = true
        };
        var userRes = await userManager.CreateAsync(adminUser, dto.AdminPassword);
        if (!userRes.Succeeded)
            throw new InvalidOperationException(string.Join("; ", userRes.Errors.Select(e => e.Description)));

        db.UserRoles.Add(new IdentityUserRole<long> { UserId = adminUser.Id, RoleId = newAdminRole.Id });
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        return new ProvisionTenantResponseDto(newTenantId, tenantName, rootOrgName, adminName);
    }

    public async Task<IReadOnlyList<TenantListItemDto>> ListAllAsync(CancellationToken ct)
    {
        var tenants = await db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);

        if (tenants.Count == 0)
            return Array.Empty<TenantListItemDto>();

        var tenantIds = tenants.Select(t => t.Id).ToList();

        var rootOrgRows = await db.Organizations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => tenantIds.Contains(o.TenantId) && o.OrgId == 1 && o.ParentId == null)
            .Select(o => new { o.TenantId, o.Name })
            .ToListAsync(ct);

        var orgByTenant = rootOrgRows
            .GroupBy(x => x.TenantId)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var adminCandidates = await (
            from u in db.Users.AsNoTracking()
            join ur in db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where tenantIds.Contains(u.TenantId)
                  && r.Name == "Admin"
                  && r.TenantId == u.TenantId
            select new { u.TenantId, u.Id, u.UserName, u.Email }
        ).ToListAsync(ct);

        var adminByTenant = adminCandidates
            .GroupBy(x => x.TenantId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.Id).First());

        var list = new List<TenantListItemDto>(tenants.Count);
        foreach (var t in tenants)
        {
            orgByTenant.TryGetValue(t.Id, out var rootOrg);
            adminByTenant.TryGetValue(t.Id, out var adm);
            list.Add(new TenantListItemDto(
                t.Id,
                t.Name,
                rootOrg ?? "",
                adm?.UserName ?? "",
                string.IsNullOrWhiteSpace(adm?.Email) ? null : adm.Email));
        }

        return list;
    }

    public async Task<TenantListItemDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var t = await db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (t is null)
            return null;

        var rootOrgName = await db.Organizations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.TenantId == id && o.OrgId == 1 && o.ParentId == null)
            .Select(o => o.Name)
            .FirstOrDefaultAsync(ct) ?? "";

        var admin = await (
            from u in db.Users.AsNoTracking()
            join ur in db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where u.TenantId == id && r.TenantId == id && r.Name == "Admin"
            orderby u.Id
            select new { u.UserName, u.Email }
        ).FirstOrDefaultAsync(ct);

        return new TenantListItemDto(
            t.Id,
            t.Name,
            rootOrgName,
            admin?.UserName ?? "",
            string.IsNullOrWhiteSpace(admin?.Email) ? null : admin.Email);
    }

    public async Task UpdateTenantNameAsync(int id, string name, CancellationToken ct)
    {
        if (id == _platform.PlatformTenantId)
            throw new InvalidOperationException("不能修改平台租户名称。");

        var trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("租户名称不能为空。", nameof(name));

        var dup = await db.Tenants.IgnoreQueryFilters()
            .AnyAsync(t => t.Name == trimmed && t.Id != id && !t.IsDeleted, ct);
        if (dup)
            throw new InvalidOperationException($"租户名称「{trimmed}」已存在。");

        var entity = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity is null)
            throw new KeyNotFoundException();
        if (entity.IsDeleted)
            throw new InvalidOperationException("该租户已删除。");

        entity.Name = trimmed;
        await db.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteTenantAsync(int id, CancellationToken ct)
    {
        if (id == _platform.PlatformTenantId)
            throw new InvalidOperationException("不能删除平台租户。");

        var entity = await db.Tenants.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity is null)
            throw new KeyNotFoundException();
        if (entity.IsDeleted)
            return;

        entity.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }
}
