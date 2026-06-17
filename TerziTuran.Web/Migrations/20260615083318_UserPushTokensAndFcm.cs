using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TerziTuran.Web.Migrations
{
    /// <inheritdoc />
    public partial class UserPushTokensAndFcm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPushTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPushTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPushTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPushTokens_Token",
                table: "UserPushTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPushTokens_UserId_IsActive_LastSeenAt",
                table: "UserPushTokens",
                columns: new[] { "UserId", "IsActive", "LastSeenAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPushTokens");
        }
    }
}
