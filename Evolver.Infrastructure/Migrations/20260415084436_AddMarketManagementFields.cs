using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evolver.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketManagementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Markets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Markets",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Markets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "Markets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RentAmount",
                table: "Markets",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Markets",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Markets");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Markets");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Markets");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "Markets");

            migrationBuilder.DropColumn(
                name: "RentAmount",
                table: "Markets");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Markets");
        }
    }
}
