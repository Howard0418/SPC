using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPortalDailyMeasurementUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId",
                table: "VariableMeasurements");

            migrationBuilder.AddColumn<DateTime>(
                name: "PortalDailyDate",
                table: "VariableMeasurements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId_PortalDailyDate",
                table: "VariableMeasurements",
                columns: new[] { "PartProcessCharacteristicId", "PortalDailyDate" },
                unique: true,
                filter: "[PortalDailyDate] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId_PortalDailyDate",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "PortalDailyDate",
                table: "VariableMeasurements");

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId",
                table: "VariableMeasurements",
                column: "PartProcessCharacteristicId");
        }
    }
}
