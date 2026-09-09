using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantQR.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionReminderLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TDA_SubscriptionReminderLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionId = table.Column<int>(type: "int", nullable: false),
                    DaysRemaining = table.Column<int>(type: "int", nullable: false),
                    ReminderType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TDA_SubscriptionReminderLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TDA_SubscriptionReminderLogs_TDA_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "TDA_Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TDA_SubscriptionReminderLogs_SubscriptionId_DaysRemaining_ReminderType",
                table: "TDA_SubscriptionReminderLogs",
                columns: new[] { "SubscriptionId", "DaysRemaining", "ReminderType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TDA_SubscriptionReminderLogs");
        }
    }
}
