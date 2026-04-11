namespace Evolver.Core.Entities;

public enum CustomerType
{
    WalkIn = 1,
    Vip = 2,
    Member = 3,
    Other = 99
}

public enum SalesOrderStatus
{
    PendingProduction = 1,
    InProduction = 2,
    PendingDispatch = 3,
    Completed = 4,
    Cancelled = 9
}

public enum PurchaseOrderStatus
{
    Draft = 1,
    Submitted = 2,
    PartiallyReceived = 3,
    Received = 4,
    Cancelled = 9
}

public enum ProductionOrderStatus
{
    Draft = 1,
    Released = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 9
}

public enum InventoryTransactionType
{
    Inbound = 1,
    Outbound = 2,
    Adjustment = 3,
    Transfer = 4,
    Count = 5,
    Sales = 6,
    ProductionConsume = 7,
    ProductionOutput = 8
}

public enum InventorySourceType
{
    None = 0,
    SalesOrder = 1,
    PurchaseOrder = 2,
    ProductionOrder = 3,
    Manual = 4,
    Opening = 5
}

public enum FinanceDocumentStatus
{
    Open = 1,
    PartiallySettled = 2,
    Settled = 3,
    WrittenOff = 9
}

public enum OperatingCostCategory
{
    Labor = 1,
    StallFee = 2,
    Fuel = 3,
    Utilities = 4,
    Other = 99
}

public enum ProfitAllocationBucket
{
    OwnerPay = 1,
    Growth = 2,
    Emergency = 3,
    Buffer = 4
}
