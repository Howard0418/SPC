using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRecheckAdjustFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdjustAction",
                table: "VariableMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AdjustAmount",
                table: "VariableMeasurements",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RecheckValue",
                table: "VariableMeasurements",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjustAction",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "AdjustAmount",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "RecheckValue",
                table: "VariableMeasurements");
        }
    }
}
