using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evolver.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameCustomerCategoryIdToCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "CustomerCategories",
                newName: "CategoryCode");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerCategories_TenantId_OrgId_CategoryId",
                table: "CustomerCategories",
                newName: "IX_CustomerCategories_TenantId_OrgId_CategoryCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CategoryCode",
                table: "CustomerCategories",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_CustomerCategories_TenantId_OrgId_CategoryCode",
                table: "CustomerCategories",
                newName: "IX_CustomerCategories_TenantId_OrgId_CategoryId");
        }
    }
}
