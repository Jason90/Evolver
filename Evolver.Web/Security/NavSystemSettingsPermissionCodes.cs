namespace Evolver.Web.Security;

/// <summary>系统设置侧栏菜单对应的按钮权限 Code（与导航菜单种子中的 nav:system:* 一致）。</summary>
public static class NavSystemSettingsPermissionCodes
{
    public static class Users
    {
        public const string Query = "nav:system:users:query";
        public const string Create = "nav:system:users:create";
        public const string Update = "nav:system:users:update";
        public const string Delete = "nav:system:users:delete";
        public const string Import = "nav:system:users:import";
        public const string Export = "nav:system:users:export";
        public const string ResetPassword = "nav:system:users:reset-password";
        public const string MoveDepartment = "nav:system:users:move-department";
    }

    public static class Dictionary
    {
        public const string Query = "nav:system:dictionary:query";
        public const string Create = "nav:system:dictionary:create";
        public const string Update = "nav:system:dictionary:update";
        public const string Delete = "nav:system:dictionary:delete";
        public const string Import = "nav:system:dictionary:import";
        public const string Export = "nav:system:dictionary:export";
    }

    public static class EnumConfig
    {
        public const string Query = "nav:system:enum-config:query";
        public const string Create = "nav:system:enum-config:create";
        public const string Update = "nav:system:enum-config:update";
        public const string Delete = "nav:system:enum-config:delete";
        public const string Import = "nav:system:enum-config:import";
        public const string Export = "nav:system:enum-config:export";
    }

    public static class Organizations
    {
        public const string Query = "nav:system:organizations:query";
        public const string Create = "nav:system:organizations:create";
        public const string Update = "nav:system:organizations:update";
        public const string Delete = "nav:system:organizations:delete";
        public const string Import = "nav:system:organizations:import";
        public const string Export = "nav:system:organizations:export";
    }

    /// <summary>侧栏「菜单权限」页（资源树）。</summary>
    public static class PermissionsPage
    {
        public const string Query = "nav:system:permissions:query";
        public const string Create = "nav:system:permissions:create";
        public const string Update = "nav:system:permissions:update";
        public const string Delete = "nav:system:permissions:delete";
        public const string Import = "nav:system:permissions:import";
        public const string Export = "nav:system:permissions:export";
    }

    public static class Roles
    {
        public const string Query = "nav:system:roles:query";
        public const string Create = "nav:system:roles:create";
        public const string Update = "nav:system:roles:update";
        public const string Delete = "nav:system:roles:delete";
        public const string Import = "nav:system:roles:import";
        public const string Export = "nav:system:roles:export";
    }

    public static class Units
    {
        public const string Query = "nav:system:units:query";
        public const string Create = "nav:system:units:create";
        public const string Update = "nav:system:units:update";
        public const string Delete = "nav:system:units:delete";
        public const string Import = "nav:system:units:import";
        public const string Export = "nav:system:units:export";
    }

    public static class Parameters
    {
        public const string Query = "nav:system:parameters:query";
        public const string Create = "nav:system:parameters:create";
        public const string Update = "nav:system:parameters:update";
        public const string Delete = "nav:system:parameters:delete";
        public const string Import = "nav:system:parameters:import";
        public const string Export = "nav:system:parameters:export";
    }

    public static class ProductCategories
    {
        public const string Query = "nav:product:categories:query";
        public const string Create = "nav:product:categories:create";
        public const string Update = "nav:product:categories:update";
        public const string Delete = "nav:product:categories:delete";
        public const string Import = "nav:product:categories:import";
        public const string Export = "nav:product:categories:export";
    }

    public static class Products
    {
        public const string Query = "nav:product:items:query";
        public const string Create = "nav:product:items:create";
        public const string Update = "nav:product:items:update";
        public const string Delete = "nav:product:items:delete";
        public const string Import = "nav:product:items:import";
        public const string Export = "nav:product:items:export";
    }

    public static class CustomerCategories
    {
        public const string Query = "nav:customer:categories:query";
        public const string Create = "nav:customer:categories:create";
        public const string Update = "nav:customer:categories:update";
        public const string Delete = "nav:customer:categories:delete";
        public const string Import = "nav:customer:categories:import";
        public const string Export = "nav:customer:categories:export";
    }

    public static class Customers
    {
        public const string Query = "nav:customer:customers:query";
        public const string Create = "nav:customer:customers:create";
        public const string Update = "nav:customer:customers:update";
        public const string Delete = "nav:customer:customers:delete";
        public const string Import = "nav:customer:customers:import";
        public const string Export = "nav:customer:customers:export";
    }

    public static class Suppliers
    {
        public const string Query = "nav:procurement:suppliers:query";
        public const string Create = "nav:procurement:suppliers:create";
        public const string Update = "nav:procurement:suppliers:update";
        public const string Delete = "nav:procurement:suppliers:delete";
        public const string Import = "nav:procurement:suppliers:import";
        public const string Export = "nav:procurement:suppliers:export";
    }

    public static class Markets
    {
        public const string Query = "nav:sales:marketing:query";
        public const string Create = "nav:sales:marketing:create";
        public const string Update = "nav:sales:marketing:update";
        public const string Delete = "nav:sales:marketing:delete";
        public const string Import = "nav:sales:marketing:import";
        public const string Export = "nav:sales:marketing:export";
    }
}
