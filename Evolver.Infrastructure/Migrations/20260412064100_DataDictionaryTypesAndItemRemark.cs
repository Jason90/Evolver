using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evolver.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DataDictionaryTypesAndItemRemark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "DataDictionaryItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DataDictionaryTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeCode = table.Column<string>(type: "TEXT", nullable: false),
                    TypeName = table.Column<string>(type: "TEXT", nullable: false),
                    Remark = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrgId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdateBy = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataDictionaryTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataDictionaryTypes_TenantId_OrgId_TypeCode",
                table: "DataDictionaryTypes",
                columns: new[] { "TenantId", "OrgId", "TypeCode" },
                unique: true);

            // 为已有字典项按 CategoryCode 补建类型行，避免列表为空。
            migrationBuilder.Sql(
                """
                INSERT INTO DataDictionaryTypes (TypeCode, TypeName, Remark, IsActive, SortOrder, TenantId, OrgId, UpdateBy, UpdateTime)
                SELECT DISTINCT i.CategoryCode, i.CategoryCode, NULL, 1, 0, i.TenantId, i.OrgId, NULL, NULL
                FROM DataDictionaryItems i
                WHERE NOT EXISTS (
                    SELECT 1 FROM DataDictionaryTypes t
                    WHERE t.TenantId = i.TenantId AND t.OrgId = i.OrgId AND t.TypeCode = i.CategoryCode
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataDictionaryTypes");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "DataDictionaryItems");
        }
    }
}
