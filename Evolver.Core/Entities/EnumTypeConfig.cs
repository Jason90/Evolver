namespace Evolver.Core.Entities;

/// <summary>
/// 枚举类型定义（对应表 dbo.EnumTypes）。
/// </summary>
public sealed class EnumTypeConfig : BaseEntity
{
    /// <summary>业务枚举类型编码，如 ProductType。</summary>
    public string EnumTypeCode { get; set; } = "";

    /// <summary>类型名称。</summary>
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<EnumValueConfig> Values { get; set; } = new List<EnumValueConfig>();
}

