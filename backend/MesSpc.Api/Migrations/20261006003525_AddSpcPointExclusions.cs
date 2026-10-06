using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSpcPointExclusions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpcPointExclusions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartProcessCharacteristicId = table.Column<int>(type: "int", nullable: false),
                    PointScope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, comment: "VariableMeasurement, AttributeMeasurement, or Subgroup."),
                    VariableMeasurementId = table.Column<long>(type: "bigint", nullable: true),
                    AttributeMeasurementId = table.Column<long>(type: "bigint", nullable: true),
                    MeasurementBatchId = table.Column<int>(type: "int", nullable: true),
                    PointKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Stable key for aggregate chart points such as Xbar subgroups."),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "ExcludedVisible or ExcludedHidden."),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Optional reason for excluding the chart point."),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true, comment: "Inactive rows are restored points kept for audit."),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpcPointExclusions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpcPointExclusions_AttributeMeasurements_AttributeMeasurementId",
                        column: x => x.AttributeMeasurementId,
                        principalTable: "AttributeMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpcPointExclusions_MeasurementBatches_MeasurementBatchId",
                        column: x => x.MeasurementBatchId,
                        principalTable: "MeasurementBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpcPointExclusions_PartProcessCharacteristics_PartProcessCharacteristicId",
                        column: x => x.PartProcessCharacteristicId,
                        principalTable: "PartProcessCharacteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpcPointExclusions_VariableMeasurements_VariableMeasurementId",
                        column: x => x.VariableMeasurementId,
                        principalTable: "VariableMeasurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Single chart point exclusion states for SPC control/trend charts.");

            migrationBuilder.CreateIndex(
                name: "IX_SpcPointExclusions_AttributeMeasurementId",
                table: "SpcPointExclusions",
                column: "AttributeMeasurementId");

            migrationBuilder.CreateIndex(
                name: "IX_SpcPointExclusions_MeasurementBatchId",
                table: "SpcPointExclusions",
                column: "MeasurementBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SpcPointExclusions_PartProcessCharacteristicId_PointKey_IsActive",
                table: "SpcPointExclusions",
                columns: new[] { "PartProcessCharacteristicId", "PointKey", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SpcPointExclusions_PointScope_AttributeMeasurementId_IsActive",
                table: "SpcPointExclusions",
                columns: new[] { "PointScope", "AttributeMeasurementId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SpcPointExclusions_PointScope_VariableMeasurementId_IsActive",
                table: "SpcPointExclusions",
                columns: new[] { "PointScope", "VariableMeasurementId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SpcPointExclusions_VariableMeasurementId",
                table: "SpcPointExclusions",
                column: "VariableMeasurementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpcPointExclusions");
        }
    }
}
