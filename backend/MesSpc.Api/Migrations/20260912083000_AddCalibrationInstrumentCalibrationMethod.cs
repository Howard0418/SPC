using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MesSpc.Api.Infrastructure.Data;

namespace MesSpc.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260912083000_AddCalibrationInstrumentCalibrationMethod")]
public partial class AddCalibrationInstrumentCalibrationMethod : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        name: "CalibrationMethod",
        table: "CalibrationInstruments",
        type: "nvarchar(100)",
        maxLength: 100,
        nullable: true,
        comment: "Calibration module CalibrationMethod");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: "CalibrationMethod",
        table: "CalibrationInstruments");
}
