namespace Evolver.Core.Entities;

/// <summary>
/// 枚举值定义（对应表 dbo.EnumValues）。
/// </summary>
public sealed class EnumValueConfig : BaseEntity
{
    public string EnumTypeCode { get; set; } = "";
    public string EnumValueCode { get; set; } = "";

    public string Name { get; set; } = "";
    public int SortNo { get; set; }
    public bool IsDefault { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public EnumTypeConfig? EnumType { get; set; }
}

