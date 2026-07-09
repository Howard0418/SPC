using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    public partial class MergeGroupsAndCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add ChartGroupId to ControlChartTypes
            migrationBuilder.AddColumn<int>(
                name: "ChartGroupId",
                table: "ControlChartTypes",
                type: "int",
                nullable: true);

            // 2. Migrate the data (copy Group ID from Category to Type)
            migrationBuilder.Sql(@"
                UPDATE ControlChartTypes
                SET ChartGroupId = (
                    SELECT ChartGroupId 
                    FROM ControlChartCategories 
                    WHERE ControlChartCategories.Id = ControlChartTypes.ChartCategoryId
                )
            ");

            // 3. Drop ForeignKey and Index on ChartCategoryId
            migrationBuilder.DropForeignKey(
                name: "FK_ControlChartTypes_ControlChartCategories_ChartCategoryId",
                table: "ControlChartTypes");

            migrationBuilder.DropIndex(
                name: "IX_ControlChartTypes_ChartCategoryId",
                table: "ControlChartTypes");

            // 4. Drop column ChartCategoryId
            migrationBuilder.DropColumn(
                name: "ChartCategoryId",
                table: "ControlChartTypes");

            // 5. Add ForeignKey and Index on ChartGroupId
            migrationBuilder.CreateIndex(
                name: "IX_ControlChartTypes_ChartGroupId",
                table: "ControlChartTypes",
                column: "ChartGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_ControlChartTypes_ControlChartGroups_ChartGroupId",
                table: "ControlChartTypes",
                column: "ChartGroupId",
                principalTable: "ControlChartGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 6. Drop ControlChartCategories table
            migrationBuilder.DropTable(
                name: "ControlChartCategories");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback logic needed for this cleanup.
        }
    }
}
