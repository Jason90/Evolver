using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evolver.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCategoryManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CustomerCategoryRefId",
                table: "Customers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Remark = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrgId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdateBy = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerCategoryRefId",
                table: "Customers",
                column: "CustomerCategoryRefId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCategories_TenantId_OrgId_CategoryId",
                table: "CustomerCategories",
                columns: new[] { "TenantId", "OrgId", "CategoryId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_CustomerCategories_CustomerCategoryRefId",
                table: "Customers",
                column: "CustomerCategoryRefId",
                principalTable: "CustomerCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_CustomerCategories_CustomerCategoryRefId",
                table: "Customers");

            migrationBuilder.DropTable(
                name: "CustomerCategories");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerCategoryRefId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerCategoryRefId",
                table: "Customers");
        }
    }
}
