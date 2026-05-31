using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evolver.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemParametersModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemParameters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ParamKey = table.Column<string>(type: "TEXT", nullable: false),
                    ParamValue = table.Column<string>(type: "TEXT", nullable: false),
                    IsSystemBuiltIn = table.Column<bool>(type: "INTEGER", nullable: false),
                    Remark = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    OrgId = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdateBy = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemParameters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemParameters_TenantId_OrgId_ParamKey",
                table: "SystemParameters",
                columns: new[] { "TenantId", "OrgId", "ParamKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemParameters");
        }
    }
}
