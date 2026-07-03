using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AjoVault.API.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteCodeAccountAndContributionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "SavingsGroups",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryPurpose",
                table: "SavingsGroups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "Contributions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Contributions",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Backfill unique invite codes for existing rows before creating the unique index
            migrationBuilder.Sql(
                "UPDATE \"SavingsGroups\" SET \"InviteCode\" = LOWER(REPLACE(\"Name\", ' ', '-')) || '-' || SUBSTR(MD5(RANDOM()::TEXT), 1, 4) WHERE \"InviteCode\" = ''");

            // Backfill account numbers for existing users
            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"AccountNumber\" = LPAD(FLOOR(RANDOM() * 9000000000 + 1000000000)::TEXT, 10, '0') WHERE \"AccountNumber\" = ''");

            // Backfill contribution status
            migrationBuilder.Sql(
                "UPDATE \"Contributions\" SET \"Status\" = 'Received' WHERE \"Status\" = ''");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsGroups_InviteCode",
                table: "SavingsGroups",
                column: "InviteCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavingsGroups_InviteCode",
                table: "SavingsGroups");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "SavingsGroups");

            migrationBuilder.DropColumn(
                name: "PrimaryPurpose",
                table: "SavingsGroups");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "Contributions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Contributions");
        }
    }
}
