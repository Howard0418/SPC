using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorAuthenticationRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Operators",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Operators",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Editor");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Operators",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Operators_Username",
                table: "Operators",
                column: "Username",
                unique: true,
                filter: "[Username] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Operators_Username",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Operators");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Operators");
        }
    }
}
