using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations;

[Migration("20260716090454_AddProcessControlScope")]
public partial class AddProcessControlScope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ControlScope",
            table: "Processes",
            type: "nvarchar(450)",
            nullable: false,
            defaultValue: "PRODUCT");

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ControlScope", table: "Processes");
    }
}
