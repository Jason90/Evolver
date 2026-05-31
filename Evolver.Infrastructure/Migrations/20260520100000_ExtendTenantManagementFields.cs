using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evolver.Infrastructure.Migrations;

/// <inheritdoc />
public partial class ExtendTenantManagementFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "CreateTime",
            table: "Tenants",
            type: "TEXT",
            nullable: true);

        migrationBuilder.Sql("UPDATE \"Tenants\" SET \"CreateTime\" = datetime('now') WHERE \"CreateTime\" IS NULL");

        migrationBuilder.AddColumn<DateTime>(
            name: "ExpireAt",
            table: "Tenants",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Remark",
            table: "Tenants",
            type: "TEXT",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CreateTime",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "ExpireAt",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "Remark",
            table: "Tenants");
    }
}

