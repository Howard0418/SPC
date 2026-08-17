using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260805130000_MoveCharacteristicUnitToControlItem")]
public partial class MoveCharacteristicUnitToControlItem : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Unit", table: "QualityCharacteristics");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Unit",
            table: "QualityCharacteristics",
            type: "nvarchar(max)",
            nullable: true);
    }
}
