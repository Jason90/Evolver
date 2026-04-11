namespace Evolver.Core.Entities;

/// <summary>运营成本录入（人工、摊位费、燃料等）。</summary>
public sealed class OperatingCostEntry : BaseEntity
{
    public OperatingCostCategory Category { get; set; }
    public decimal Amount { get; set; }
    public DateOnly CostDate { get; set; }
    public string? PeriodKey { get; set; }
    public string? Description { get; set; }
}
