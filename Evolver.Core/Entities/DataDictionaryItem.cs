namespace Evolver.Core.Entities;

/// <summary>
/// Data dictionary entry (e.g. units, product tags) scoped by category code.
/// </summary>
public sealed class DataDictionaryItem : BaseEntity
{
    /// <summary>Logical group, e.g. ProductUnit, CustomerType.</summary>
    public string CategoryCode { get; set; } = "";

    public string ItemCode { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? ItemValue { get; set; }

    /// <summary>备注（若依字典数据中的「备注」）。</summary>
    public string? Remark { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
