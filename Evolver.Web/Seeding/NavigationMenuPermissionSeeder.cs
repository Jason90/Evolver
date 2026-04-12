using Evolver.Core.Entities;
using Evolver.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Seeding;

/// <summary>
/// 与 Blazor 侧栏导航（NavMenu）一致的菜单资源树，并在每个菜单页下挂通用按钮权限。
/// </summary>
public static class NavigationMenuPermissionSeeder
{
    private sealed record SeedRow(
        string? ParentCode,
        string Code,
        string Name,
        PermissionType Type,
        int SortOrder,
        string? Resource,
        string? ComponentPath);

    private static IEnumerable<SeedRow> BuildRows()
    {
        var rows = new List<SeedRow>
        {
            new(null, "nav:dashboard", "仪表盘", PermissionType.Menu, 10, "/", "Pages/Home.razor"),
            new(null, "nav:system", "系统管理", PermissionType.Directory, 20, null, null),
            new("nav:system", "nav:system:tenants", "租户管理", PermissionType.Menu, 10, "settings/tenants", "Pages/Settings/TenantProvisioning.razor"),
            new("nav:system", "nav:system:organizations", "组织架构", PermissionType.Menu, 20, "settings/organizations", "Pages/Settings/OrganizationManagement.razor"),
            new("nav:system", "nav:system:users", "用户管理", PermissionType.Menu, 30, "settings/users", "Pages/Settings/UserManagement.razor"),
            new("nav:system", "nav:system:roles", "角色管理", PermissionType.Menu, 40, "settings/roles", "Pages/Settings/RoleManagement.razor"),
            new("nav:system", "nav:system:permissions", "菜单权限", PermissionType.Menu, 50, "settings/permissions", "Pages/Settings/PermissionManagement.razor"),
            new("nav:system", "nav:system:dictionary", "字典管理", PermissionType.Menu, 60, "settings/data-dictionary", "Pages/Settings/DataDictionaryManagement.razor"),
            new("nav:system", "nav:system:parameters", "参数设置", PermissionType.Menu, 70, "settings/parameters", "Pages/Settings/ApplicationParameters.razor"),
            new(null, "nav:customer", "客户管理", PermissionType.Directory, 30, null, null),
            new("nav:customer", "nav:customer:customers", "客户信息", PermissionType.Menu, 10, "customers", "Pages/Customers.razor"),
            new(null, "nav:product", "商品管理", PermissionType.Directory, 40, null, null),
            new("nav:product", "nav:product:items", "商品信息", PermissionType.Menu, 10, "products", "Pages/Products.razor"),
            new("nav:product", "nav:product:bom", "物料清单", PermissionType.Menu, 20, "product-master", "Pages/ProductMaster.razor"),
            new(null, "nav:procurement", "采购管理", PermissionType.Directory, 50, null, null),
            new("nav:procurement", "nav:procurement:suppliers", "供应商信息", PermissionType.Menu, 10, "procurement/suppliers", "Pages/Purchasing/SupplierDirectory.razor"),
            new("nav:procurement", "nav:procurement:purchase-orders", "采购单", PermissionType.Menu, 20, "procurement/purchase-orders", "Pages/Purchasing/PurchaseOrders.razor"),
            new(null, "nav:production", "生产管理", PermissionType.Directory, 60, null, null),
            new("nav:production", "nav:production:orders", "生产单", PermissionType.Menu, 10, "production", "Pages/Production.razor"),
            new(null, "nav:sales", "销售管理", PermissionType.Directory, 70, null, null),
            new("nav:sales", "nav:sales:marketing", "市场管理", PermissionType.Menu, 10, "sales/marketing", "Pages/Sales/Marketing.razor"),
            new("nav:sales", "nav:sales:forecast", "销售预测", PermissionType.Menu, 20, "sales/forecast", "Pages/Sales/SalesForecast.razor"),
            new("nav:sales", "nav:sales:orders", "销售单", PermissionType.Menu, 30, "orders", "Pages/Orders.razor"),
            new(null, "nav:finance", "财务管理", PermissionType.Directory, 80, null, null),
            new("nav:finance", "nav:finance:receivables", "应收账款", PermissionType.Menu, 10, "finance/receivables", "Pages/Finances/Receivables.razor"),
            new("nav:finance", "nav:finance:payables", "应付账款", PermissionType.Menu, 20, "finance/payables", "Pages/Finances/Payables.razor"),
            new("nav:finance", "nav:finance:cost", "成本管理", PermissionType.Menu, 30, "finance/cost", "Pages/Finances/CostManagement.razor"),
            new(null, "nav:statistics", "统计分析", PermissionType.Directory, 90, null, null),
            new("nav:statistics", "nav:statistics:inventory", "库存统计", PermissionType.Menu, 10, "statistics/inventory", "Pages/Statistics/InventoryStatistics.razor"),
            new("nav:statistics", "nav:statistics:procurement", "采购统计", PermissionType.Menu, 20, "statistics/procurement", "Pages/Statistics/ProcurementStatistics.razor"),
            new("nav:statistics", "nav:statistics:production", "生产统计", PermissionType.Menu, 30, "statistics/production", "Pages/Statistics/ProductionStatistics.razor"),
            new("nav:statistics", "nav:statistics:sales", "销售统计", PermissionType.Menu, 40, "statistics/sales", "Pages/Statistics/SalesStatistics.razor"),
            new("nav:statistics", "nav:statistics:cashflow", "收支分析", PermissionType.Menu, 50, "statistics/cashflow", "Pages/Statistics/CashFlowAnalysis.razor"),
            new("nav:statistics", "nav:statistics:operations", "经营分析", PermissionType.Menu, 60, "statistics/operations", "Pages/Statistics/OperationsAnalysis.razor"),
        };

        var actions = new (string Suffix, string Name)[]
        {
            ("query", "查询"),
            ("create", "新增"),
            ("update", "修改"),
            ("delete", "删除"),
            ("import", "导入"),
            ("export", "导出"),
        };

        // 必须先 ToList()：不能在枚举 rows 的同时向 rows 追加（会触发 Collection was modified）。
        foreach (var m in rows.Where(r => r.Type == PermissionType.Menu).ToList())
        {
            var o = 0;
            foreach (var (suffix, label) in actions)
            {
                o += 10;
                rows.Add(new SeedRow(m.Code, $"{m.Code}:{suffix}", label, PermissionType.UiButton, o, null, null));
            }
        }

        return rows;
    }

    public static async Task EnsureCatalogAsync(AppDbContext db, int tenantId, int orgId, CancellationToken ct = default)
    {
        var pending = BuildRows().ToList();
        var idByCode = await db.Permissions.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.OrgId == orgId)
            .ToDictionaryAsync(p => p.Code, p => p.Id, ct);

        var safety = 0;
        while (pending.Count > 0 && safety++ < 10_000)
        {
            var progressed = false;
            for (var i = pending.Count - 1; i >= 0; i--)
            {
                var row = pending[i];
                long? parentId = null;
                if (row.ParentCode is not null)
                {
                    if (!idByCode.TryGetValue(row.ParentCode, out var pid))
                        continue;
                    parentId = pid;
                }

                if (idByCode.ContainsKey(row.Code))
                {
                    pending.RemoveAt(i);
                    progressed = true;
                    continue;
                }

                var entity = new Permission
                {
                    TenantId = tenantId,
                    OrgId = orgId,
                    ParentId = parentId,
                    Type = row.Type,
                    Code = row.Code,
                    Name = row.Name,
                    Resource = row.Resource,
                    ComponentPath = row.ComponentPath,
                    SortOrder = row.SortOrder,
                    IsEnabled = true,
                    IsExternalLink = false,
                    IsVisible = true,
                };
                db.Permissions.Add(entity);
                await db.SaveChangesAsync(ct);
                idByCode[row.Code] = entity.Id;
                pending.RemoveAt(i);
                progressed = true;
            }

            if (!progressed)
                throw new InvalidOperationException("无法解析菜单权限父级，请检查 ParentCode 定义。");
        }
    }
}
