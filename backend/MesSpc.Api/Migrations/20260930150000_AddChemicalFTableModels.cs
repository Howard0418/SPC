using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    [Migration("20260930150000_AddChemicalFTableModels")]
    public partial class AddChemicalFTableModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChemicalFTableVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourcePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EffectiveAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalFTableVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChemicalFTableCells",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    SheetName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CellAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedCellAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StandardSolution = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NumericValue = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    TextValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FormulaText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalFTableCells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChemicalFTableCells_ChemicalFTableVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "ChemicalFTableVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChemicalFTableReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    PartProcessCharacteristicId = table.Column<int>(type: "int", nullable: false),
                    CellAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedCellAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceFormula = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceSheet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceRow = table.Column<int>(type: "int", nullable: true),
                    ReferenceContext = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalFTableReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChemicalFTableReferences_ChemicalFTableVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "ChemicalFTableVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChemicalFTableReferences_PartProcessCharacteristics_PartProcessCharacteristicId",
                        column: x => x.PartProcessCharacteristicId,
                        principalTable: "PartProcessCharacteristics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFTableCells_VersionId_NormalizedCellAddress",
                table: "ChemicalFTableCells",
                columns: new[] { "VersionId", "NormalizedCellAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFTableReferences_PartProcessCharacteristicId",
                table: "ChemicalFTableReferences",
                column: "PartProcessCharacteristicId");

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFTableReferences_VersionId_NormalizedCellAddress",
                table: "ChemicalFTableReferences",
                columns: new[] { "VersionId", "NormalizedCellAddress" });

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFTableReferences_VersionId_PartProcessCharacteristicId_NormalizedCellAddress",
                table: "ChemicalFTableReferences",
                columns: new[] { "VersionId", "PartProcessCharacteristicId", "NormalizedCellAddress" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFTableVersions_VersionCode",
                table: "ChemicalFTableVersions",
                column: "VersionCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ChemicalFTableCells");
            migrationBuilder.DropTable(name: "ChemicalFTableReferences");
            migrationBuilder.DropTable(name: "ChemicalFTableVersions");
        }
    }
}
