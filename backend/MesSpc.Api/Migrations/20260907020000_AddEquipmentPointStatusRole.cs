using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MesSpc.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260907020000_AddEquipmentPointStatusRole")]
public class AddEquipmentPointStatusRole : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "StatusRole", table: "EquipmentPointMappings", maxLength: 20, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ActiveWhen", table: "EquipmentPointMappings", maxLength: 20, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ActiveWhen", table: "EquipmentPointMappings");
        migrationBuilder.DropColumn(name: "StatusRole", table: "EquipmentPointMappings");
    }
}
