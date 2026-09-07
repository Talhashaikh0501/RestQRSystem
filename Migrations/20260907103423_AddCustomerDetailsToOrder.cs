using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantQR.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerDetailsToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "TDA_Orders",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                table: "TDA_Orders",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "TDA_Orders");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                table: "TDA_Orders");
        }
    }
}
