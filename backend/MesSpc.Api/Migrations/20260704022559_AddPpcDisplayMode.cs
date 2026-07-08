using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPpcDisplayMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayMode",
                table: "PartProcessCharacteristics",
                type: "nvarchar(30)",
                nullable: false,
                defaultValue: "CONTROL_CHART");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayMode",
                table: "PartProcessCharacteristics");
        }
    }
}
