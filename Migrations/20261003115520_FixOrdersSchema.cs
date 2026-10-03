using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FurniCraft.Migrations
{
    public partial class FixOrdersSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename existing columns to match the current Order model
            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "Orders",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "DetailedAddress",
                table: "Orders",
                newName: "Address");

            // The old database stores Status as int,
            // while the current Order model uses string.
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convert Status back to int.
            // There are currently no orders in the database.
            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Orders",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Orders",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Orders",
                newName: "DetailedAddress");
        }
    }
}