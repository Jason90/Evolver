namespace Evolver.Core.Entities;

public sealed class ProductCategory : BaseEntity
{
    public long? ParentId { get; set; }
    public ProductCategory? Parent { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    /// <summary>Raw material, finished good, semi-finished, kit, etc.</summary>
    public string? CategoryKind { get; set; }
}
