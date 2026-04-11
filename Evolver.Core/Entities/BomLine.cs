namespace Evolver.Core.Entities;

public sealed class BomLine : BaseEntity
{
    public long BomHeaderId { get; set; }
    public BomHeader BomHeader { get; set; } = null!;

    public long ComponentProductId { get; set; }
    public Product ComponentProduct { get; set; } = null!;

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "";
    public int SortOrder { get; set; }
    public decimal? ScrapRate { get; set; }
}
