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
}
