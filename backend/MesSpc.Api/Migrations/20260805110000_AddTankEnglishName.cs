using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260805110000_AddTankEnglishName")]
public partial class AddTankEnglishName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TankNameEn",
            table: "Tanks",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "TankNameEn", table: "Tanks");
    }
}
