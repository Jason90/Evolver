namespace Evolver.Core.Entities;

/// <summary>应付 — 对供应商负债。</summary>
public sealed class AccountsPayable : BaseEntity
{
    public string DocumentNo { get; set; } = "";
    public long? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public long? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public decimal Amount { get; set; }
    public decimal SettledAmount { get; set; }
    public DateOnly DueDate { get; set; }
    public FinanceDocumentStatus Status { get; set; } = FinanceDocumentStatus.Open;
}
