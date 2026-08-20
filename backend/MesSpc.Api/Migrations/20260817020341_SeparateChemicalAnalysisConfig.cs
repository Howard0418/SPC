using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeparateChemicalAnalysisConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChemicalAnalysisConfigJson",
                table: "PartProcessCharacteristics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE PartProcessCharacteristics
                SET ChemicalAnalysisConfigJson = JSON_QUERY(FormulaConfigJson, '$.ChemicalAnalysis'),
                    FormulaConfigJson = CASE
                        WHEN JSON_MODIFY(FormulaConfigJson, '$.ChemicalAnalysis', NULL) = '{}' THEN NULL
                        ELSE JSON_MODIFY(FormulaConfigJson, '$.ChemicalAnalysis', NULL)
                    END
                WHERE ISJSON(FormulaConfigJson) = 1
                  AND JSON_QUERY(FormulaConfigJson, '$.ChemicalAnalysis') IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE PartProcessCharacteristics
                SET FormulaConfigJson = JSON_MODIFY(COALESCE(FormulaConfigJson, '{}'), '$.ChemicalAnalysis', JSON_QUERY(ChemicalAnalysisConfigJson))
                WHERE ISJSON(ChemicalAnalysisConfigJson) = 1;
                """);

            migrationBuilder.DropColumn(
                name: "ChemicalAnalysisConfigJson",
                table: "PartProcessCharacteristics");
        }
    }
}
