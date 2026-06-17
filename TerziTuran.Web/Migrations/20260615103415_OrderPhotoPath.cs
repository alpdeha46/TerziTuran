using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TerziTuran.Web.Migrations
{
    /// <inheritdoc />
    public partial class OrderPhotoPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "Orders",
                type: "TEXT",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "Orders");
        }
    }
}
