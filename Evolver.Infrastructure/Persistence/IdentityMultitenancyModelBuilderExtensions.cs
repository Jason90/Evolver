using Evolver.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Infrastructure.Persistence;

/// <summary>
/// 将 Identity 默认的「全局唯一用户名 / 角色名」改为「租户内唯一」，以支持多租户共用同一显示名（如 Admin）。
/// </summary>
internal static class IdentityMultitenancyModelBuilderExtensions
{
    public static void ApplyMultiTenantIdentityIndexes(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(b =>
        {
            b.HasIndex(u => u.NormalizedUserName)
                .HasDatabaseName("UserNameIndex")
                .IsUnique(false);

            b.HasIndex(u => new { u.TenantId, u.NormalizedUserName }).IsUnique();

            // 保留 EmailIndex 便于查询；不按 (TenantId, Email) 唯一——空邮箱在多用户场景下常见，SQLite 下唯一索引易与历史数据冲突。
            b.HasIndex(u => u.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique(false);
        });
    }
}
