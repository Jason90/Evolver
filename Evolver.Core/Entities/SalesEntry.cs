namespace Evolver.Core.Entities;

public sealed class SalesEntry : BaseEntity
{
    public DateOnly Date { get; set; }

    public long MarketId { get; set; }
    public Market Market { get; set; } = null!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal UnitsSold { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SalesValue { get; set; }
    public string? Notes { get; set; }
}
