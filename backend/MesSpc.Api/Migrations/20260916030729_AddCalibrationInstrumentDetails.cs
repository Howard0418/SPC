using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCalibrationInstrumentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptanceCriteria",
                table: "CalibrationInstruments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                comment: "校正模組 AcceptanceCriteria");

            migrationBuilder.AddColumn<string>(
                name: "CalibrationStandard",
                table: "CalibrationInstruments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                comment: "校正模組 CalibrationStandard");

            migrationBuilder.AddColumn<string>(
                name: "MeasurementSpecification",
                table: "CalibrationInstruments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                comment: "校正模組 MeasurementSpecification");

            migrationBuilder.AddColumn<string>(
                name: "Precision",
                table: "CalibrationInstruments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                comment: "校正模組 Precision");

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "CalibrationInstruments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                comment: "校正模組 Remarks");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MeasurementSpecification", table: "CalibrationInstruments");
            migrationBuilder.DropColumn(name: "Precision", table: "CalibrationInstruments");
            migrationBuilder.DropColumn(name: "Remarks", table: "CalibrationInstruments");
            migrationBuilder.DropColumn(name: "CalibrationStandard", table: "CalibrationInstruments");
            migrationBuilder.DropColumn(name: "AcceptanceCriteria", table: "CalibrationInstruments");
        }
    }
}
