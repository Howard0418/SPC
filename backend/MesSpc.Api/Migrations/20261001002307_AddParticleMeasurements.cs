using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddParticleMeasurements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParticleMeasurements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Upload batch that produced this measurement."),
                    MeasurementTime = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Measurement timestamp normalized to UTC."),
                    Location = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, comment: "Particle monitoring location code, such as R1."),
                    ParticleSize = table.Column<decimal>(type: "decimal(6,3)", nullable: false, comment: "Particle size in micrometers."),
                    Count = table.Column<long>(type: "bigint", nullable: false, comment: "Non-negative particle count."),
                    SamplingVolume = table.Column<decimal>(type: "decimal(18,6)", nullable: true, comment: "Optional sampling volume."),
                    SamplingDurationSeconds = table.Column<decimal>(type: "decimal(18,3)", nullable: true, comment: "Optional sampling duration in seconds."),
                    DeviceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "Optional particle counter device code."),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "Optional measurement remark."),
                    SourceSheet = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, comment: "Original Excel worksheet name."),
                    SourceRow = table.Column<int>(type: "int", nullable: false, comment: "Original Excel row number."),
                    SourceColumn = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, comment: "Original Excel column label."),
                    RawValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false, comment: "Original cell value before normalization."),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticleMeasurements", x => x.Id);
                    table.CheckConstraint("CK_ParticleMeasurements_Count_NonNegative", "[Count] >= 0");
                    table.ForeignKey(
                        name: "FK_ParticleMeasurements_UploadBatches_UploadBatchId",
                        column: x => x.UploadBatchId,
                        principalTable: "UploadBatches",
                        principalColumn: "UploadBatchId",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Particle monitoring measurements stored in long format.");

            migrationBuilder.CreateIndex(
                name: "IX_ParticleMeasurements_Location_ParticleSize_MeasurementTime",
                table: "ParticleMeasurements",
                columns: new[] { "Location", "ParticleSize", "MeasurementTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ParticleMeasurements_ParticleSize_MeasurementTime_Location",
                table: "ParticleMeasurements",
                columns: new[] { "ParticleSize", "MeasurementTime", "Location" });

            migrationBuilder.CreateIndex(
                name: "IX_ParticleMeasurements_UploadBatchId",
                table: "ParticleMeasurements",
                column: "UploadBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticleMeasurements_UploadBatchId_SourceSheet_SourceRow_SourceColumn",
                table: "ParticleMeasurements",
                columns: new[] { "UploadBatchId", "SourceSheet", "SourceRow", "SourceColumn" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ParticleMeasurements");
        }
    }
}
