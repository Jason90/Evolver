namespace Evolver.Core.Entities;

/// <summary>
/// 字典类型（若依「枚举配置」中的类型行），与 <see cref="DataDictionaryItem"/> 的 <c>CategoryCode</c> 对应。
/// </summary>
public sealed class DataDictionaryType : BaseEntity
{
    /// <summary>唯一类型编码，如 sys_user_sex。</summary>
    public string TypeCode { get; set; } = "";

    /// <summary>显示名称，如「用户性别」。</summary>
    public string TypeName { get; set; } = "";

    public string? Remark { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
