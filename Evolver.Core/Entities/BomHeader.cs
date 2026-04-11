namespace Evolver.Core.Entities;

/// <summary>BOM revision for a finished / semi-finished product.</summary>
public sealed class BomHeader : BaseEntity
{
    public long FinishedProductId { get; set; }
    public Product FinishedProduct { get; set; } = null!;

    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public ICollection<BomLine> Lines { get; set; } = new List<BomLine>();
}
