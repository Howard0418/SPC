using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MesSpc.Api.Infrastructure.Data;

namespace MesSpc.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260912090000_MakeCalibrationNextDateOptional")]
public partial class MakeCalibrationNextDateOptional : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("ALTER TABLE [CalibrationInstruments] ALTER COLUMN [NextCalibrationDate] date NULL;");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("ALTER TABLE [CalibrationInstruments] ALTER COLUMN [NextCalibrationDate] date NOT NULL;");
}
