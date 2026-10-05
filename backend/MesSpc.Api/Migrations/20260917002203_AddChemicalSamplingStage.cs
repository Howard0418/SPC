using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChemicalSamplingStage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId_PortalDailyDate_SamplingPhase",
                table: "VariableMeasurements");

            migrationBuilder.AddColumn<string>(
                name: "SamplingStage",
                table: "VariableMeasurements",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "GENERAL");

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId_PortalDailyDate_SamplingPhase_SamplingStage",
                table: "VariableMeasurements",
                columns: new[] { "PartProcessCharacteristicId", "PortalDailyDate", "SamplingPhase", "SamplingStage" },
                unique: true,
                filter: "[PortalDailyDate] IS NOT NULL");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId_PortalDailyDate_SamplingPhase_SamplingStage",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "SamplingStage",
                table: "VariableMeasurements");

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId_PortalDailyDate_SamplingPhase",
                table: "VariableMeasurements",
                columns: new[] { "PartProcessCharacteristicId", "PortalDailyDate", "SamplingPhase" },
                unique: true,
                filter: "[PortalDailyDate] IS NOT NULL");
        }
    }
}
