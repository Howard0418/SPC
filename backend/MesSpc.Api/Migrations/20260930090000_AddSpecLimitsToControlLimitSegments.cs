using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    public partial class AddSpecLimitsToControlLimitSegments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "USL",
                table: "ControlLimitSegments",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LSL",
                table: "ControlLimitSegments",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TargetValue",
                table: "ControlLimitSegments",
                type: "float",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "USL", table: "ControlLimitSegments");
            migrationBuilder.DropColumn(name: "LSL", table: "ControlLimitSegments");
            migrationBuilder.DropColumn(name: "TargetValue", table: "ControlLimitSegments");
        }
    }
}
