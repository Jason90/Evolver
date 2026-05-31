namespace Evolver.Frontend.Navigation;

using Microsoft.AspNetCore.Components;

public sealed record BreadcrumbItem(string Title, string? Href);

/// <summary>根据当前路由解析顶部面包屑（与左侧 NavMenu 结构一致）。</summary>
public static class NavigationTrail
{
    private static readonly BreadcrumbItem Home = new("首页", "/");

    private static readonly Dictionary<string, BreadcrumbItem[]> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["settings/tenants"] = [Home, Dir("系统管理"), Page("租户管理")],
        ["settings/organizations"] = [Home, Dir("系统管理"), Page("组织架构")],
        ["settings/users"] = [Home, Dir("系统管理"), Page("用户管理")],
        ["settings/roles"] = [Home, Dir("系统管理"), Page("角色管理")],
        ["settings/permissions"] = [Home, Dir("系统管理"), Page("菜单权限")],
        ["settings/data-dictionary"] = [Home, Dir("系统管理"), Page("数据字典")],
        ["settings/data-dictionary/items"] = [Home, Dir("系统管理"), Link("数据字典", "/settings/data-dictionary"), Page("字典项")],
        ["settings/enum-config"] = [Home, Dir("系统管理"), Page("枚举配置")],
        ["settings/parameters"] = [Home, Dir("系统管理"), Page("参数设置")],
        ["settings/units"] = [Home, Dir("系统管理"), Page("单位维护")],
        ["settings"] = [Home, Dir("系统管理"), Page("系统设置")],
        ["customer-categories"] = [Home, Dir("客户管理"), Page("客户类别")],
        ["customers"] = [Home, Dir("客户管理"), Page("客户信息")],
        ["products"] = [Home, Dir("商品管理"), Page("商品信息")],
        ["product-master"] = [Home, Dir("商品管理"), Page("物料清单")],
        ["product-categories"] = [Home, Dir("商品管理"), Page("商品类别")],
        ["procurement/suppliers"] = [Home, Dir("采购管理"), Page("供应商信息")],
        ["procurement/purchase-orders"] = [Home, Dir("采购管理"), Page("采购单")],
        ["procurement"] = [Home, Dir("采购管理"), Page("采购管理")],
        ["production"] = [Home, Dir("生产管理"), Page("生产单")],
        ["sales/marketing"] = [Home, Dir("销售管理"), Page("市场管理")],
        ["sales/forecast"] = [Home, Dir("销售管理"), Page("销售预测")],
        ["orders"] = [Home, Dir("销售管理"), Page("销售单")],
        ["orders/kanban"] = [Home, Dir("销售管理"), Link("销售单", "/orders"), Page("看板")],
        ["finance/receivables"] = [Home, Dir("财务管理"), Page("应收账款")],
        ["finance/payables"] = [Home, Dir("财务管理"), Page("应付账款")],
        ["finance/cost"] = [Home, Dir("财务管理"), Page("成本管理")],
        ["finance"] = [Home, Dir("财务管理"), Page("财务管理")],
        ["statistics/inventory"] = [Home, Dir("统计分析"), Page("库存统计")],
        ["statistics/procurement"] = [Home, Dir("统计分析"), Page("采购统计")],
        ["statistics/production"] = [Home, Dir("统计分析"), Page("生产统计")],
        ["statistics/sales"] = [Home, Dir("统计分析"), Page("销售统计")],
        ["statistics/cashflow"] = [Home, Dir("统计分析"), Page("收支分析")],
        ["statistics/operations"] = [Home, Dir("统计分析"), Page("经营分析")],
        ["account"] = [Home, Page("我的帐户")],
        ["account/password"] = [Home, Link("我的帐户", "/account"), Page("修改密码")],
    };

    public static IReadOnlyList<BreadcrumbItem> Resolve(NavigationManager navigation)
    {
        var relative = navigation.ToBaseRelativePath(navigation.Uri).Trim('/');
        if (string.IsNullOrEmpty(relative))
            return [Home];

        if (Map.TryGetValue(relative, out var exact))
            return exact;

        var prefixKey = Map.Keys
            .Where(k => relative.StartsWith(k + "/", StringComparison.OrdinalIgnoreCase)
                        || relative.Equals(k, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(k => k.Length)
            .FirstOrDefault();

        if (prefixKey is not null && Map.TryGetValue(prefixKey, out var prefixTrail))
            return prefixTrail;

        var segment = relative.Split('/').LastOrDefault() ?? relative;
        return [Home, new(segment, null)];
    }

    private static BreadcrumbItem Dir(string title) => new(title, null);

    private static BreadcrumbItem Page(string title) => new(title, null);

    private static BreadcrumbItem Link(string title, string href) => new(title, href);
}
