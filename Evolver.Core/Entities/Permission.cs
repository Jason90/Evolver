namespace Evolver.Core.Entities;

public enum PermissionType
{
    Api = 1,
    UiButton = 2,
    /// <summary>侧栏分组（目录），不直接对应页面。</summary>
    Directory = 3,
    /// <summary>路由菜单项。</summary>
    Menu = 4,
}

public sealed class Permission : BaseEntity
{
    public long? ParentId { get; set; }
    public Permission? Parent { get; set; }

    public PermissionType Type { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>路由或 API 资源说明；菜单存相对路径。</summary>
    public string? Resource { get; set; }
    /// <summary>前端组件路径（如 Pages/Settings/UserManagement.razor）。</summary>
    public string? ComponentPath { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    /// <summary>Bootstrap Icons 类名片段，如 <c>bi-gear</c>。</summary>
    public string? Icon { get; set; }
    public bool IsExternalLink { get; set; }
    /// <summary>是否在菜单中显示（隐藏仍保留权限数据）。</summary>
    public bool IsVisible { get; set; } = true;
}
