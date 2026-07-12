using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AjoVault.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDvaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DvaAccountName",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DvaAccountNumber",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DvaBankName",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "KredarCustomerId",
                table: "Users",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DvaAccountName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DvaAccountNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DvaBankName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KredarCustomerId",
                table: "Users");
        }
    }
}
