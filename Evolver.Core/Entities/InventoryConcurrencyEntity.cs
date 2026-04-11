namespace Evolver.Core.Entities;

/// <summary>
/// Base for inventory snapshot-style rows that use optimistic concurrency (RowVersion).
/// </summary>
public abstract class InventoryConcurrencyEntity : BaseEntity
{
    public byte[] RowVersion { get; set; } = null!;
}
