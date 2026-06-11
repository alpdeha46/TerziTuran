using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TerziTuran.Web.Migrations
{
    /// <inheritdoc />
    public partial class CodeRequestDispatchTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserPasswordRequests_UserId_RequestType_IsUsed",
                table: "UserPasswordRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "DispatchedAt",
                table: "UserPasswordRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDispatched",
                table: "UserPasswordRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_UserPasswordRequests_UserId_RequestType_IsUsed_IsDispatched",
                table: "UserPasswordRequests",
                columns: new[] { "UserId", "RequestType", "IsUsed", "IsDispatched" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserPasswordRequests_UserId_RequestType_IsUsed_IsDispatched",
                table: "UserPasswordRequests");

            migrationBuilder.DropColumn(
                name: "DispatchedAt",
                table: "UserPasswordRequests");

            migrationBuilder.DropColumn(
                name: "IsDispatched",
                table: "UserPasswordRequests");

            migrationBuilder.CreateIndex(
                name: "IX_UserPasswordRequests_UserId_RequestType_IsUsed",
                table: "UserPasswordRequests",
                columns: new[] { "UserId", "RequestType", "IsUsed" });
        }
    }
}
