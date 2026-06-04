using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TerziTuran.Web.Migrations
{
    /// <inheritdoc />
    public partial class BagReceiptAndTurkishPolish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BagReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PickupCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    BagCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    IsDelivered = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BagReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BagReceipts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BagReceipts_OrderId",
                table: "BagReceipts",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_BagReceipts_PickupCode",
                table: "BagReceipts",
                column: "PickupCode");

            migrationBuilder.CreateIndex(
                name: "IX_BagReceipts_ReceiptNumber",
                table: "BagReceipts",
                column: "ReceiptNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BagReceipts");
        }
    }
}
