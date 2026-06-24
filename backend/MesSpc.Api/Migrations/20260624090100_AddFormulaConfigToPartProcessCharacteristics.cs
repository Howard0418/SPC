using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260624090100_AddFormulaConfigToPartProcessCharacteristics")]
    public partial class AddFormulaConfigToPartProcessCharacteristics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormulaConfigJson",
                table: "PartProcessCharacteristics",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormulaConfigJson",
                table: "PartProcessCharacteristics");
        }
    }
}
