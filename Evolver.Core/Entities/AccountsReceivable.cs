namespace Evolver.Core.Entities;

/// <summary>应收 — 客户欠款。</summary>
public sealed class AccountsReceivable : BaseEntity
{
    public string DocumentNo { get; set; } = "";
    public long? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public long? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public decimal Amount { get; set; }
    public decimal SettledAmount { get; set; }
    public DateOnly DueDate { get; set; }
    public FinanceDocumentStatus Status { get; set; } = FinanceDocumentStatus.Open;
}
