using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MesSpc.Api.Infrastructure.Data;

namespace MesSpc.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260912080000_AddCalibrationInstrumentLocation")]
public partial class AddCalibrationInstrumentLocation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        name: "Location",
        table: "CalibrationInstruments",
        type: "nvarchar(100)",
        maxLength: 100,
        nullable: true,
        comment: "®Õ¥¿¼Ò²Õ Location");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: "Location",
        table: "CalibrationInstruments");
}
