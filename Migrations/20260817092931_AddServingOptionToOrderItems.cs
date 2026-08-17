using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantQR.Migrations
{
    /// <inheritdoc />
    public partial class AddServingOptionToOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MenuItemOptionId",
                table: "TDA_OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionName",
                table: "TDA_OrderItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TDA_OrderItems_MenuItemOptionId",
                table: "TDA_OrderItems",
                column: "MenuItemOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_TDA_OrderItems_TDA_MenuItemOptions_MenuItemOptionId",
                table: "TDA_OrderItems",
                column: "MenuItemOptionId",
                principalTable: "TDA_MenuItemOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TDA_OrderItems_TDA_MenuItemOptions_MenuItemOptionId",
                table: "TDA_OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_TDA_OrderItems_MenuItemOptionId",
                table: "TDA_OrderItems");

            migrationBuilder.DropColumn(
                name: "MenuItemOptionId",
                table: "TDA_OrderItems");

            migrationBuilder.DropColumn(
                name: "OptionName",
                table: "TDA_OrderItems");
        }
    }
}
