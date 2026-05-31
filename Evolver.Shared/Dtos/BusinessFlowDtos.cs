namespace Evolver.Shared.Dtos;

public sealed record SalesForecastLineDto(
    string Market,
    long ProductId,
    string ProductCode,
    string ProductName,
    decimal HistoricalUnits,
    bool IsBestSeller,
    decimal ForecastUnits,
    decimal SuggestedDeltaPercent);

public sealed record ForecastRequestDto(
    string? Market,
    DateOnly? FromDate,
    DateOnly? ToDate);

public sealed record BomRequirementItemDto(
    long ComponentProductId,
    string ComponentCode,
    string ComponentName,
    decimal RequiredQty,
    decimal CurrentStock,
    decimal ShortageQty);

public sealed record PlannedSalesItemDto(
    long ProductId,
    decimal Quantity);

public sealed record BomRequirementRequestDto(
    IReadOnlyList<PlannedSalesItemDto> PlannedSales);

public sealed record PurchaseSuggestionLineDto(
    long ProductId,
    string ProductCode,
    string ProductName,
    decimal CurrentStock,
    decimal RequiredQty,
    decimal SuggestPurchaseQty,
    decimal UnitCost);

public sealed record PurchaseOrderCreateRequestDto(
    string? SupplierCode,
    string? SupplierName,
    IReadOnlyList<PurchaseSuggestionLineDto> Lines);

public sealed record SalesOrderCreateLineDto(
    long ProductId,
    decimal Quantity,
    decimal UnitPrice);

public sealed record SalesOrderCreateRequestDto(
    string MarketName,
    string CustomerName,
    string? SalesUserName,
    bool CheckoutNow,
    bool EnforceMaterialCheck,
    string? Notes,
    IReadOnlyList<SalesOrderCreateLineDto> Lines);

public sealed record MaterialShortageDto(
    long ProductId,
    string ProductCode,
    string ProductName,
    decimal RequiredQty,
    decimal AvailableQty,
    decimal ShortageQty,
    decimal UnitCost);

public sealed record SimpleOrderLineDto(
    long ProductId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal Amount);

public sealed record SalesOrderResultDto(
    long SalesOrderId,
    string SalesOrderNo,
    string Status,
    decimal TotalAmount,
    long? ProductionOrderId,
    string? ProductionOrderNo,
    IReadOnlyList<MaterialShortageDto> MaterialShortages);

public sealed record PurchaseOrderSummaryDto(
    long Id,
    string OrderNo,
    string SupplierName,
    string Status,
    DateOnly OrderDate,
    decimal TotalAmount);

public sealed record PurchaseReceiveResultDto(
    long PurchaseOrderId,
    string PurchaseOrderNo,
    decimal ReceivedAmount,
    string Status,
    string AccountsPayableNo);

public sealed record PurchaseReceiveRequestDto(
    IReadOnlyList<PurchaseReceiveLineDto> Lines);

public sealed record PurchaseReceiveLineDto(
    long ProductId,
    decimal ReceiveQuantity);

public sealed record SalesOrderSummaryDto(
    long Id,
    string OrderNo,
    string CustomerName,
    string OrderStatus,
    DateOnly OrderDate,
    string SalesUserName,
    string PaymentStatus,
    decimal TotalQuantity,
    decimal TotalAmount,
    decimal ReceivedAmount,
    string? Notes,
    string? UpdateByUserName,
    DateTime? UpdateTime);

public sealed record SalesOrderLineDetailDto(
    long ProductId,
    string ProductCode,
    string ProductName,
    decimal? UnitCost,
    decimal? SuggestedPrice,
    decimal Quantity,
    decimal DiscountRate,
    decimal UnitPrice,
    decimal Amount,
    string? Remark
);

public sealed record SalesOrderDetailDto(
    long SalesOrderId,
    string OrderNo,
    string CustomerName,
    string OrderStatus,
    DateOnly OrderDate,
    string SalesUserName,
    string PaymentStatus,
    decimal TotalQuantity,
    decimal TotalAmount,
    decimal ReceivedAmount,
    string? Notes,
    string? UpdateByUserName,
    DateTime? UpdateTime,
    IReadOnlyList<SalesOrderLineDetailDto> Lines
);

public sealed record SalesOrderProductOptionDto(
    long ProductId,
    string ProductCode,
    string ProductName,
    decimal? UnitCost,
    decimal? SuggestedPrice
);

public sealed record SalesOrderAmountScaleDto(
    int AmountDecimalPlaces
);

public sealed record OperationsAnalysisQueryDto(
    string Range,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string GroupBy);

public sealed record OperationsAnalysisRowDto(
    string GroupKey,
    decimal SalesQuantity,
    decimal SalesAmount,
    decimal ProfitRate,
    decimal LaborCostRate,
    decimal SalesCostRate);
