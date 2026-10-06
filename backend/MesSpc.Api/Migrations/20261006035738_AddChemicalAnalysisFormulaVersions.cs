using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChemicalAnalysisFormulaVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChemicalAnalysisFormulaVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartProcessCharacteristicId = table.Column<int>(type: "int", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviousConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "Create, Update, or Restore."),
                    RestoredFromVersionId = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalAnalysisFormulaVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChemicalAnalysisFormulaVersions_ChemicalAnalysisFormulaVersions_RestoredFromVersionId",
                        column: x => x.RestoredFromVersionId,
                        principalTable: "ChemicalAnalysisFormulaVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChemicalAnalysisFormulaVersions_PartProcessCharacteristics_PartProcessCharacteristicId",
                        column: x => x.PartProcessCharacteristicId,
                        principalTable: "PartProcessCharacteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Version history for chemical analysis formulas stored on PartProcessCharacteristics.");

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalAnalysisFormulaVersions_PartProcessCharacteristicId_ChangedAt",
                table: "ChemicalAnalysisFormulaVersions",
                columns: new[] { "PartProcessCharacteristicId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalAnalysisFormulaVersions_PartProcessCharacteristicId_VersionNo",
                table: "ChemicalAnalysisFormulaVersions",
                columns: new[] { "PartProcessCharacteristicId", "VersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalAnalysisFormulaVersions_RestoredFromVersionId",
                table: "ChemicalAnalysisFormulaVersions",
                column: "RestoredFromVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChemicalAnalysisFormulaVersions");
        }
    }
}
