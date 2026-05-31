using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evolver.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnumConfigTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnumTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnumTypeCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrgId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdateBy = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnumTypes", x => x.Id);
                    table.UniqueConstraint("AK_EnumTypes_TenantId_OrgId_EnumTypeCode", x => new { x.TenantId, x.OrgId, x.EnumTypeCode });
                });

            migrationBuilder.CreateTable(
                name: "EnumValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnumTypeCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EnumValueCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SortNo = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrgId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdateBy = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnumValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnumValues_EnumTypes_TenantId_OrgId_EnumTypeCode",
                        columns: x => new { x.TenantId, x.OrgId, x.EnumTypeCode },
                        principalTable: "EnumTypes",
                        principalColumns: new[] { "TenantId", "OrgId", "EnumTypeCode" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnumTypes_TenantId_OrgId_EnumTypeCode",
                table: "EnumTypes",
                columns: new[] { "TenantId", "OrgId", "EnumTypeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnumValues_TenantId_OrgId_EnumTypeCode_EnumValueCode",
                table: "EnumValues",
                columns: new[] { "TenantId", "OrgId", "EnumTypeCode", "EnumValueCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnumValues_TenantId_OrgId_EnumTypeCode_SortNo",
                table: "EnumValues",
                columns: new[] { "TenantId", "OrgId", "EnumTypeCode", "SortNo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnumValues");

            migrationBuilder.DropTable(
                name: "EnumTypes");
        }
    }
}
