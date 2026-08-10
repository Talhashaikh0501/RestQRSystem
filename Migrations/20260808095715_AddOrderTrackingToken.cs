using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantQR.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTrackingToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrackingToken",
                table: "Orders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrackingToken",
                table: "Orders");
        }
    }
}
