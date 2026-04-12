using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Infrastructure.Persistence;

public sealed partial class AppDbContext : IdentityDbContext<AppUser, AppRole, long>
{
    private readonly ITenantContext _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
        : base(options) =>
        _tenant = tenant;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<UserOrganization> UserOrganizations => Set<UserOrganization>();

    public DbSet<DataDictionaryItem> DataDictionaryItems => Set<DataDictionaryItem>();
    public DbSet<DataDictionaryType> DataDictionaryTypes => Set<DataDictionaryType>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();

    public DbSet<BomHeader> BomHeaders => Set<BomHeader>();
    public DbSet<BomLine> BomLines => Set<BomLine>();

    public DbSet<InventorySnapshot> InventorySnapshots => Set<InventorySnapshot>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    public DbSet<Market> Markets => Set<Market>();
    public DbSet<SalesEntry> SalesEntries => Set<SalesEntry>();
    public DbSet<MenuIntelligenceRecord> MenuIntelligenceRecords => Set<MenuIntelligenceRecord>();
    public DbSet<MarketInventorySnapshot> MarketInventorySnapshots => Set<MarketInventorySnapshot>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<ProductionOrderMaterialLine> ProductionOrderMaterialLines => Set<ProductionOrderMaterialLine>();
    public DbSet<ProductionWasteRecord> ProductionWasteRecords => Set<ProductionWasteRecord>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<MembershipAccount> MembershipAccounts => Set<MembershipAccount>();
    public DbSet<MembershipPointsLedger> MembershipPointsLedgers => Set<MembershipPointsLedger>();

    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<OrderOperationLog> OrderOperationLogs => Set<OrderOperationLog>();

    public DbSet<AccountsReceivable> AccountsReceivables => Set<AccountsReceivable>();
    public DbSet<AccountsPayable> AccountsPayables => Set<AccountsPayable>();
    public DbSet<OperatingCostEntry> OperatingCostEntries => Set<OperatingCostEntry>();
    public DbSet<ProfitAllocationRun> ProfitAllocationRuns => Set<ProfitAllocationRun>();
    public DbSet<ProfitAllocationLine> ProfitAllocationLines => Set<ProfitAllocationLine>();

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.ApplyMultiTenantIdentityIndexes();

        ApplyTenantOrgFilters(modelBuilder);
        ApplyTenantRowQueryFilter(modelBuilder);
    }
}
