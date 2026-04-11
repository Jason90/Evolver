namespace Evolver.Core.Entities;

/// <summary>一次利润核算与分配批次（如按月）。</summary>
public sealed class ProfitAllocationRun : BaseEntity
{
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
    public decimal OperatingCostTotal { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProfitAllocationLine> Lines { get; set; } = new List<ProfitAllocationLine>();
}
