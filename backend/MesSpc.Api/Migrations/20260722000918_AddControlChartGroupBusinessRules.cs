using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddControlChartGroupBusinessRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessScopeCode",
                table: "ControlChartGroups",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresMachine",
                table: "ControlChartGroups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPart",
                table: "ControlChartGroups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresTank",
                table: "ControlChartGroups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE ControlChartGroups
                SET BusinessScopeCode = CASE
                        WHEN UPPER(GroupCode) IN ('CHEM', 'CHEM_TREND') THEN 'CHEMICAL'
                        WHEN UPPER(GroupCode) = 'PROD' THEN 'PRODUCT'
                        WHEN UPPER(GroupCode) = 'PROC' THEN 'PROCESS'
                        ELSE UPPER(GroupCode)
                    END,
                    RequiresPart = CASE WHEN UPPER(GroupCode) = 'PROD' THEN 1 ELSE 0 END,
                    RequiresMachine = CASE WHEN UPPER(GroupCode) IN ('CHEM', 'CHEM_TREND') THEN 1 ELSE 0 END,
                    RequiresTank = CASE WHEN UPPER(GroupCode) IN ('CHEM', 'CHEM_TREND') THEN 1 ELSE 0 END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ControlChartGroups_BusinessScopeCode",
                table: "ControlChartGroups",
                column: "BusinessScopeCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ControlChartGroups_BusinessScopeCode",
                table: "ControlChartGroups");

            migrationBuilder.DropColumn(
                name: "BusinessScopeCode",
                table: "ControlChartGroups");

            migrationBuilder.DropColumn(
                name: "RequiresMachine",
                table: "ControlChartGroups");

            migrationBuilder.DropColumn(
                name: "RequiresPart",
                table: "ControlChartGroups");

            migrationBuilder.DropColumn(
                name: "RequiresTank",
                table: "ControlChartGroups");
        }
    }
}
