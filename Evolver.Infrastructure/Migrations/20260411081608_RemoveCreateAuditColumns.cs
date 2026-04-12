using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evolver.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCreateAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "UserOrganizations");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "UserOrganizations");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "SalesOrderLines");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "SalesOrderLines");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "SalesEntries");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "SalesEntries");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "PurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "PurchaseOrderLines");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "ProfitAllocationRuns");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "ProfitAllocationRuns");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "ProfitAllocationLines");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "ProfitAllocationLines");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "ProductionWasteRecords");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "ProductionWasteRecords");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "ProductionOrderMaterialLines");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "ProductionOrderMaterialLines");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "OrderOperationLogs");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "OrderOperationLogs");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "OperatingCostEntries");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "OperatingCostEntries");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "MenuIntelligenceRecords");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "MenuIntelligenceRecords");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "MembershipPointsLedgers");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "MembershipPointsLedgers");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "MembershipAccounts");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "MembershipAccounts");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "Markets");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Markets");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "MarketInventorySnapshots");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "MarketInventorySnapshots");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "InventorySnapshots");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "InventorySnapshots");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "DataDictionaryItems");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "DataDictionaryItems");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "BomLines");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "BomLines");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "BomHeaders");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "BomHeaders");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "AccountsReceivables");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "AccountsReceivables");

            migrationBuilder.DropColumn(
                name: "CreateBy",
                table: "AccountsPayables");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "AccountsPayables");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "UserOrganizations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "UserOrganizations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "Tenants",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Tenants",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "Suppliers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Suppliers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "SalesOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "SalesOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "SalesOrderLines",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "SalesOrderLines",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "SalesEntries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "SalesEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "RolePermissions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "RolePermissions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "PurchaseOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "PurchaseOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "PurchaseOrderLines",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "PurchaseOrderLines",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "ProfitAllocationRuns",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "ProfitAllocationRuns",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "ProfitAllocationLines",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "ProfitAllocationLines",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "Products",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "ProductionWasteRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "ProductionWasteRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "ProductionOrders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "ProductionOrders",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "ProductionOrderMaterialLines",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "ProductionOrderMaterialLines",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "ProductCategories",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "ProductCategories",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "Permissions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Permissions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "Organizations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Organizations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "OrderOperationLogs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "OrderOperationLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "OperatingCostEntries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "OperatingCostEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "MenuIntelligenceRecords",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "MenuIntelligenceRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "MembershipPointsLedgers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "MembershipPointsLedgers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "MembershipAccounts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "MembershipAccounts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "Markets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Markets",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "MarketInventorySnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "MarketInventorySnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "InventoryTransactions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "InventoryTransactions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "InventorySnapshots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "InventorySnapshots",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "DataDictionaryItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "DataDictionaryItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "Customers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "BomLines",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "BomLines",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "BomHeaders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "BomHeaders",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "AspNetRoles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "AspNetRoles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "AccountsReceivables",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "AccountsReceivables",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreateBy",
                table: "AccountsPayables",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "AccountsPayables",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
