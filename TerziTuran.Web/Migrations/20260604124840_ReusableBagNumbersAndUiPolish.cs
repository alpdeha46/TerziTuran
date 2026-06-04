using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TerziTuran.Web.Migrations
{
    /// <inheritdoc />
    public partial class ReusableBagNumbersAndUiPolish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BagNumber",
                table: "BagReceipts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BagReceipts_BagNumber",
                table: "BagReceipts",
                column: "BagNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BagReceipts_BagNumber",
                table: "BagReceipts");

            migrationBuilder.DropColumn(
                name: "BagNumber",
                table: "BagReceipts");
        }
    }
}
