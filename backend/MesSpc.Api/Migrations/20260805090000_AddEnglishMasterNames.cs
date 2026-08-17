using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260805090000_AddEnglishMasterNames")]
public partial class AddEnglishMasterNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ProcessNameEn", table: "Processes", type: "nvarchar(max)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "CharacteristicNameEn", table: "QualityCharacteristics", type: "nvarchar(max)", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ProcessNameEn", table: "Processes");
        migrationBuilder.DropColumn(name: "CharacteristicNameEn", table: "QualityCharacteristics");
    }
}
