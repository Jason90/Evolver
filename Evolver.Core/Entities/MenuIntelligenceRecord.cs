namespace Evolver.Core.Entities;

public sealed class MenuIntelligenceRecord : BaseEntity
{
    public DateOnly AsOfDate { get; set; }

    public long MarketId { get; set; }
    public Market Market { get; set; } = null!;

    public int Rank { get; set; }
    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal PlannedOrders { get; set; }
    public decimal Price { get; set; }
    public decimal UnitCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal TotalProfit { get; set; }
    public string MenuAction { get; set; } = "";
}
