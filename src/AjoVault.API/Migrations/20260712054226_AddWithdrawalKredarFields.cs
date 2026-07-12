using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AjoVault.API.Migrations
{
    /// <inheritdoc />
    public partial class AddWithdrawalKredarFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Withdrawals",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KredarReference",
                table: "Withdrawals",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Withdrawals");

            migrationBuilder.DropColumn(
                name: "KredarReference",
                table: "Withdrawals");
        }
    }
}
