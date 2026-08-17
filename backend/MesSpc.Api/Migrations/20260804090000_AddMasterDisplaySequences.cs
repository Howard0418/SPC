using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260804090000_AddMasterDisplaySequences")]
public partial class AddMasterDisplaySequences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "SequenceNo", table: "Processes", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>(name: "SequenceNo", table: "Tanks", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>(name: "SequenceNo", table: "PartProcessCharacteristics", type: "int", nullable: false, defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SequenceNo", table: "Processes");
        migrationBuilder.DropColumn(name: "SequenceNo", table: "Tanks");
        migrationBuilder.DropColumn(name: "SequenceNo", table: "PartProcessCharacteristics");
    }
}
