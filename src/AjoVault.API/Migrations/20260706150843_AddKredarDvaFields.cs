using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AjoVault.API.Migrations
{
    /// <inheritdoc />
    public partial class AddKredarDvaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DvaAccountName",
                table: "SavingsGroups",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DvaAccountNumber",
                table: "SavingsGroups",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DvaBankName",
                table: "SavingsGroups",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "KredarCustomerId",
                table: "SavingsGroups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "KredarDvaId",
                table: "SavingsGroups",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DvaAccountName",
                table: "SavingsGroups");

            migrationBuilder.DropColumn(
                name: "DvaAccountNumber",
                table: "SavingsGroups");

            migrationBuilder.DropColumn(
                name: "DvaBankName",
                table: "SavingsGroups");

            migrationBuilder.DropColumn(
                name: "KredarCustomerId",
                table: "SavingsGroups");

            migrationBuilder.DropColumn(
                name: "KredarDvaId",
                table: "SavingsGroups");
        }
    }
}
