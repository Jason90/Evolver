using Evolver.Core.Entities;
using Evolver.Infrastructure.Persistence;
using Evolver.Web.Security;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Seeding;

/// <summary>
/// 将历史系统设置类 API 权限（<c>users.*</c>、<c>dictionary.*</c> 等）迁移到对应 <c>nav:system:*</c> 按钮权限并删除旧行。
/// </summary>
public static class LegacySystemApiToNavPermissionMigration
{
    private static readonly string[] LegacyCodes =
    [
        "users.read", "users.write",
        "dictionary.read", "dictionary.write",
        "organizations.read", "organizations.write",
        "permissions.read", "permissions.write",
        "roles.read", "roles.write",
    ];

    private static string[]? ReplacementCodes(string legacyCode) =>
        legacyCode switch
        {
            "users.read" => [NavSystemSettingsPermissionCodes.Users.Query, NavSystemSettingsPermissionCodes.Users.Export],
            "users.write" =>
            [
                NavSystemSettingsPermissionCodes.Users.Create,
                NavSystemSettingsPermissionCodes.Users.Update,
                NavSystemSettingsPermissionCodes.Users.Delete,
                NavSystemSettingsPermissionCodes.Users.Import,
                NavSystemSettingsPermissionCodes.Users.Export,
            ],
            "dictionary.read" => [NavSystemSettingsPermissionCodes.Dictionary.Query],
            "dictionary.write" =>
            [
                NavSystemSettingsPermissionCodes.Dictionary.Create,
                NavSystemSettingsPermissionCodes.Dictionary.Update,
                NavSystemSettingsPermissionCodes.Dictionary.Delete,
                NavSystemSettingsPermissionCodes.Dictionary.Import,
                NavSystemSettingsPermissionCodes.Dictionary.Export,
            ],
            "organizations.read" => [NavSystemSettingsPermissionCodes.Organizations.Query],
            "organizations.write" =>
            [
                NavSystemSettingsPermissionCodes.Organizations.Create,
                NavSystemSettingsPermissionCodes.Organizations.Update,
                NavSystemSettingsPermissionCodes.Organizations.Delete,
                NavSystemSettingsPermissionCodes.Organizations.Import,
                NavSystemSettingsPermissionCodes.Organizations.Export,
            ],
            "permissions.read" => [NavSystemSettingsPermissionCodes.PermissionsPage.Query],
            "permissions.write" =>
            [
                NavSystemSettingsPermissionCodes.PermissionsPage.Create,
                NavSystemSettingsPermissionCodes.PermissionsPage.Update,
                NavSystemSettingsPermissionCodes.PermissionsPage.Delete,
                NavSystemSettingsPermissionCodes.PermissionsPage.Import,
                NavSystemSettingsPermissionCodes.PermissionsPage.Export,
            ],
            "roles.read" => [NavSystemSettingsPermissionCodes.Roles.Query],
            "roles.write" =>
            [
                NavSystemSettingsPermissionCodes.Roles.Create,
                NavSystemSettingsPermissionCodes.Roles.Update,
                NavSystemSettingsPermissionCodes.Roles.Delete,
                NavSystemSettingsPermissionCodes.Roles.Import,
                NavSystemSettingsPermissionCodes.Roles.Export,
            ],
            _ => null,
        };

    public static async Task MigrateAsync(AppDbContext db, CancellationToken ct = default)
    {
        var legacyPerms = await db.Permissions.IgnoreQueryFilters()
            .Where(p => LegacyCodes.Contains(p.Code))
            .ToListAsync(ct);

        if (legacyPerms.Count == 0)
            return;

        foreach (var leg in legacyPerms)
        {
            var replacementCodes = ReplacementCodes(leg.Code);
            if (replacementCodes is null || replacementCodes.Length == 0)
                continue;

            var targets = await db.Permissions.IgnoreQueryFilters()
                .Where(p => p.TenantId == leg.TenantId && p.OrgId == leg.OrgId && replacementCodes.Contains(p.Code))
                .ToListAsync(ct);

            if (targets.Count == 0)
                continue;

            var links = await db.RolePermissions.IgnoreQueryFilters()
                .Where(rp => rp.PermissionId == leg.Id)
                .ToListAsync(ct);

            foreach (var link in links)
            {
                foreach (var target in targets)
                {
                    var exists = await db.RolePermissions.IgnoreQueryFilters().AnyAsync(
                        rp => rp.TenantId == link.TenantId
                              && rp.OrgId == link.OrgId
                              && rp.RoleId == link.RoleId
                              && rp.PermissionId == target.Id,
                        ct);
                    if (exists)
                        continue;

                    db.RolePermissions.Add(new RolePermission
                    {
                        TenantId = link.TenantId,
                        OrgId = link.OrgId,
                        RoleId = link.RoleId,
                        PermissionId = target.Id,
                    });
                }

                db.RolePermissions.Remove(link);
            }

            db.Permissions.Remove(leg);

            // 每条旧权限处理完即落库，避免同一角色先后迁移 users.read / users.write 时
            // 对 nav:*:export 等重叠目标重复 Add（未 Save 前 AnyAsync 看不到已跟踪的新行 → UNIQUE 冲突）。
            await db.SaveChangesAsync(ct);
        }
    }
}
