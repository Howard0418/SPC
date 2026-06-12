using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddControlChartTypeRuleGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RuleGroupId",
                table: "ControlChartTypes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ControlChartTypes_RuleGroupId",
                table: "ControlChartTypes",
                column: "RuleGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_ControlChartTypes_SpcRuleGroups_RuleGroupId",
                table: "ControlChartTypes",
                column: "RuleGroupId",
                principalTable: "SpcRuleGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ControlChartTypes_SpcRuleGroups_RuleGroupId",
                table: "ControlChartTypes");

            migrationBuilder.DropIndex(
                name: "IX_ControlChartTypes_RuleGroupId",
                table: "ControlChartTypes");

            migrationBuilder.DropColumn(
                name: "RuleGroupId",
                table: "ControlChartTypes");
        }
    }
}
