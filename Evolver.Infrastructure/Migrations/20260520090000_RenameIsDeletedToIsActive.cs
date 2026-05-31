using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evolver.Infrastructure.Migrations;

/// <inheritdoc />
public partial class RenameIsDeletedToIsActive : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: "AspNetRoles",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: "Tenants",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.Sql("UPDATE \"AspNetRoles\" SET \"IsActive\" = CASE WHEN \"IsDeleted\" = 1 THEN 0 ELSE 1 END;");
        migrationBuilder.Sql("UPDATE \"Tenants\" SET \"IsActive\" = CASE WHEN \"IsDeleted\" = 1 THEN 0 ELSE 1 END;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsActive",
            table: "AspNetRoles");

        migrationBuilder.DropColumn(
            name: "IsActive",
            table: "Tenants");
    }
}

