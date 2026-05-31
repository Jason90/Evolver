using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evolver.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierInfoModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SupplierType",
                table: "Suppliers",
                newName: "Website");

            migrationBuilder.RenameColumn(
                name: "ContactPhone",
                table: "Suppliers",
                newName: "Phone");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Suppliers",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "Suppliers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "Suppliers");

            migrationBuilder.RenameColumn(
                name: "Website",
                table: "Suppliers",
                newName: "SupplierType");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Suppliers",
                newName: "ContactPhone");
        }
    }
}
