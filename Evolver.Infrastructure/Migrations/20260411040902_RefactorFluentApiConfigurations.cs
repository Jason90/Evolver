using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evolver.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorFluentApiConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountsPayables_PurchaseOrders_PurchaseOrderId",
                table: "AccountsPayables");

            migrationBuilder.DropForeignKey(
                name: "FK_AccountsPayables_Suppliers_SupplierId",
                table: "AccountsPayables");

            migrationBuilder.DropForeignKey(
                name: "FK_AccountsReceivables_Customers_CustomerId",
                table: "AccountsReceivables");

            migrationBuilder.DropForeignKey(
                name: "FK_AccountsReceivables_SalesOrders_SalesOrderId",
                table: "AccountsReceivables");

            migrationBuilder.DropForeignKey(
                name: "FK_BomHeaders_Products_FinishedProductId",
                table: "BomHeaders");

            migrationBuilder.DropForeignKey(
                name: "FK_BomLines_Products_ComponentProductId",
                table: "BomLines");

            migrationBuilder.DropForeignKey(
                name: "FK_InventorySnapshots_Products_ProductId",
                table: "InventorySnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Products_ProductId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_MarketInventorySnapshots_Markets_MarketId",
                table: "MarketInventorySnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_MarketInventorySnapshots_Products_ProductId",
                table: "MarketInventorySnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_MembershipAccounts_Customers_CustomerId",
                table: "MembershipAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_MembershipPointsLedgers_Customers_CustomerId",
                table: "MembershipPointsLedgers");

            migrationBuilder.DropForeignKey(
                name: "FK_MembershipPointsLedgers_SalesOrders_SalesOrderId",
                table: "MembershipPointsLedgers");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuIntelligenceRecords_Markets_MarketId",
                table: "MenuIntelligenceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuIntelligenceRecords_Products_ProductId",
                table: "MenuIntelligenceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Organizations_ParentId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Permissions_ParentId",
                table: "Permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_ProductCategories_ParentId",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrderMaterialLines_Products_MaterialProductId",
                table: "ProductionOrderMaterialLines");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_Products_OutputProductId",
                table: "ProductionOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionWasteRecords_Products_ProductId",
                table: "ProductionWasteRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductCategories_ProductCategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLines_Products_ProductId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesEntries_Markets_MarketId",
                table: "SalesEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesEntries_Products_ProductId",
                table: "SalesEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderLines_Products_ProductId",
                table: "SalesOrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Customers_CustomerId",
                table: "SalesOrders");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountsPayables_PurchaseOrders_PurchaseOrderId",
                table: "AccountsPayables",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountsPayables_Suppliers_SupplierId",
                table: "AccountsPayables",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountsReceivables_Customers_CustomerId",
                table: "AccountsReceivables",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountsReceivables_SalesOrders_SalesOrderId",
                table: "AccountsReceivables",
                column: "SalesOrderId",
                principalTable: "SalesOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BomHeaders_Products_FinishedProductId",
                table: "BomHeaders",
                column: "FinishedProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BomLines_Products_ComponentProductId",
                table: "BomLines",
                column: "ComponentProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventorySnapshots_Products_ProductId",
                table: "InventorySnapshots",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Products_ProductId",
                table: "InventoryTransactions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MarketInventorySnapshots_Markets_MarketId",
                table: "MarketInventorySnapshots",
                column: "MarketId",
                principalTable: "Markets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MarketInventorySnapshots_Products_ProductId",
                table: "MarketInventorySnapshots",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MembershipAccounts_Customers_CustomerId",
                table: "MembershipAccounts",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MembershipPointsLedgers_Customers_CustomerId",
                table: "MembershipPointsLedgers",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MembershipPointsLedgers_SalesOrders_SalesOrderId",
                table: "MembershipPointsLedgers",
                column: "SalesOrderId",
                principalTable: "SalesOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuIntelligenceRecords_Markets_MarketId",
                table: "MenuIntelligenceRecords",
                column: "MarketId",
                principalTable: "Markets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuIntelligenceRecords_Products_ProductId",
                table: "MenuIntelligenceRecords",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Organizations_ParentId",
                table: "Organizations",
                column: "ParentId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Permissions_ParentId",
                table: "Permissions",
                column: "ParentId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_ProductCategories_ParentId",
                table: "ProductCategories",
                column: "ParentId",
                principalTable: "ProductCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrderMaterialLines_Products_MaterialProductId",
                table: "ProductionOrderMaterialLines",
                column: "MaterialProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_Products_OutputProductId",
                table: "ProductionOrders",
                column: "OutputProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionWasteRecords_Products_ProductId",
                table: "ProductionWasteRecords",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductCategories_ProductCategoryId",
                table: "Products",
                column: "ProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLines_Products_ProductId",
                table: "PurchaseOrderLines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesEntries_Markets_MarketId",
                table: "SalesEntries",
                column: "MarketId",
                principalTable: "Markets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesEntries_Products_ProductId",
                table: "SalesEntries",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderLines_Products_ProductId",
                table: "SalesOrderLines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Customers_CustomerId",
                table: "SalesOrders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountsPayables_PurchaseOrders_PurchaseOrderId",
                table: "AccountsPayables");

            migrationBuilder.DropForeignKey(
                name: "FK_AccountsPayables_Suppliers_SupplierId",
                table: "AccountsPayables");

            migrationBuilder.DropForeignKey(
                name: "FK_AccountsReceivables_Customers_CustomerId",
                table: "AccountsReceivables");

            migrationBuilder.DropForeignKey(
                name: "FK_AccountsReceivables_SalesOrders_SalesOrderId",
                table: "AccountsReceivables");

            migrationBuilder.DropForeignKey(
                name: "FK_BomHeaders_Products_FinishedProductId",
                table: "BomHeaders");

            migrationBuilder.DropForeignKey(
                name: "FK_BomLines_Products_ComponentProductId",
                table: "BomLines");

            migrationBuilder.DropForeignKey(
                name: "FK_InventorySnapshots_Products_ProductId",
                table: "InventorySnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Products_ProductId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_MarketInventorySnapshots_Markets_MarketId",
                table: "MarketInventorySnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_MarketInventorySnapshots_Products_ProductId",
                table: "MarketInventorySnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_MembershipAccounts_Customers_CustomerId",
                table: "MembershipAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_MembershipPointsLedgers_Customers_CustomerId",
                table: "MembershipPointsLedgers");

            migrationBuilder.DropForeignKey(
                name: "FK_MembershipPointsLedgers_SalesOrders_SalesOrderId",
                table: "MembershipPointsLedgers");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuIntelligenceRecords_Markets_MarketId",
                table: "MenuIntelligenceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuIntelligenceRecords_Products_ProductId",
                table: "MenuIntelligenceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_Organizations_ParentId",
                table: "Organizations");

            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Permissions_ParentId",
                table: "Permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_ProductCategories_ParentId",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrderMaterialLines_Products_MaterialProductId",
                table: "ProductionOrderMaterialLines");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_Products_OutputProductId",
                table: "ProductionOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionWasteRecords_Products_ProductId",
                table: "ProductionWasteRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductCategories_ProductCategoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderLines_Products_ProductId",
                table: "PurchaseOrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesEntries_Markets_MarketId",
                table: "SalesEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesEntries_Products_ProductId",
                table: "SalesEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderLines_Products_ProductId",
                table: "SalesOrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Customers_CustomerId",
                table: "SalesOrders");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountsPayables_PurchaseOrders_PurchaseOrderId",
                table: "AccountsPayables",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountsPayables_Suppliers_SupplierId",
                table: "AccountsPayables",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountsReceivables_Customers_CustomerId",
                table: "AccountsReceivables",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountsReceivables_SalesOrders_SalesOrderId",
                table: "AccountsReceivables",
                column: "SalesOrderId",
                principalTable: "SalesOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BomHeaders_Products_FinishedProductId",
                table: "BomHeaders",
                column: "FinishedProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BomLines_Products_ComponentProductId",
                table: "BomLines",
                column: "ComponentProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventorySnapshots_Products_ProductId",
                table: "InventorySnapshots",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Products_ProductId",
                table: "InventoryTransactions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MarketInventorySnapshots_Markets_MarketId",
                table: "MarketInventorySnapshots",
                column: "MarketId",
                principalTable: "Markets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MarketInventorySnapshots_Products_ProductId",
                table: "MarketInventorySnapshots",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MembershipAccounts_Customers_CustomerId",
                table: "MembershipAccounts",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MembershipPointsLedgers_Customers_CustomerId",
                table: "MembershipPointsLedgers",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MembershipPointsLedgers_SalesOrders_SalesOrderId",
                table: "MembershipPointsLedgers",
                column: "SalesOrderId",
                principalTable: "SalesOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuIntelligenceRecords_Markets_MarketId",
                table: "MenuIntelligenceRecords",
                column: "MarketId",
                principalTable: "Markets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuIntelligenceRecords_Products_ProductId",
                table: "MenuIntelligenceRecords",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_Organizations_ParentId",
                table: "Organizations",
                column: "ParentId",
                principalTable: "Organizations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Permissions_ParentId",
                table: "Permissions",
                column: "ParentId",
                principalTable: "Permissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_ProductCategories_ParentId",
                table: "ProductCategories",
                column: "ParentId",
                principalTable: "ProductCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrderMaterialLines_Products_MaterialProductId",
                table: "ProductionOrderMaterialLines",
                column: "MaterialProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_Products_OutputProductId",
                table: "ProductionOrders",
                column: "OutputProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionWasteRecords_Products_ProductId",
                table: "ProductionWasteRecords",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductCategories_ProductCategoryId",
                table: "Products",
                column: "ProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderLines_Products_ProductId",
                table: "PurchaseOrderLines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesEntries_Markets_MarketId",
                table: "SalesEntries",
                column: "MarketId",
                principalTable: "Markets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesEntries_Products_ProductId",
                table: "SalesEntries",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderLines_Products_ProductId",
                table: "SalesOrderLines",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Customers_CustomerId",
                table: "SalesOrders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
