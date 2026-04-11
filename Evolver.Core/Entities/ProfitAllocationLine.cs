namespace Evolver.Core.Entities;

/// <summary>利润分配明细（Owner 30% / Growth 20% / Emergency 10% / Buffer 40% 等）。</summary>
public sealed class ProfitAllocationLine : BaseEntity
{
    public long ProfitAllocationRunId { get; set; }
    public ProfitAllocationRun Run { get; set; } = null!;

    public ProfitAllocationBucket Bucket { get; set; }
    public decimal Ratio { get; set; }
    public decimal Amount { get; set; }
}
