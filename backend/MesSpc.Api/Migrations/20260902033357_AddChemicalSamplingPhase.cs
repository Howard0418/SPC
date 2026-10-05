using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChemicalSamplingPhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId_PortalDailyDate",
                table: "VariableMeasurements");

            migrationBuilder.AddColumn<string>(
                name: "SamplingPhase",
                table: "VariableMeasurements",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "GENERAL");

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId_PortalDailyDate_SamplingPhase",
                table: "VariableMeasurements",
                columns: new[] { "PartProcessCharacteristicId", "PortalDailyDate", "SamplingPhase" },
                unique: true,
                filter: "[PortalDailyDate] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId_PortalDailyDate_SamplingPhase",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "SamplingPhase",
                table: "VariableMeasurements");

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId_PortalDailyDate",
                table: "VariableMeasurements",
                columns: new[] { "PartProcessCharacteristicId", "PortalDailyDate" },
                unique: true,
                filter: "[PortalDailyDate] IS NOT NULL");
        }
    }
}
