using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_MesSyncAndTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "AlertEvents");

            migrationBuilder.DropColumn(
                name: "MeasurementValueId",
                table: "AlertEvents");

            migrationBuilder.RenameColumn(
                name: "StationId",
                table: "AlertEvents",
                newName: "ProcessId");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "AlertEvents",
                newName: "PartId");

            migrationBuilder.RenameColumn(
                name: "InspectionItemId",
                table: "AlertEvents",
                newName: "CharacteristicId");

            migrationBuilder.AddColumn<int>(
                name: "ChemicalId",
                table: "VariableMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SideCode",
                table: "VariableMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                table: "VariableMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "VariableMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChemicalId",
                table: "AttributeMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SideCode",
                table: "AttributeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                table: "AttributeMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "AttributeMeasurements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "AttributeMeasurementId",
                table: "AlertEvents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UploadBatchId",
                table: "AlertEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VariableMeasurementId",
                table: "AlertEvents",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Chemicals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChemicalCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChemicalName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChemicalType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chemicals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MesSyncMessages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SyncStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MesSyncMessages", x => x.MessageId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_ChemicalId",
                table: "VariableMeasurements",
                column: "ChemicalId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeMeasurements_ChemicalId",
                table: "AttributeMeasurements",
                column: "ChemicalId");

            migrationBuilder.CreateIndex(
                name: "IX_Chemicals_ChemicalCode",
                table: "Chemicals",
                column: "ChemicalCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeMeasurements_Chemicals_ChemicalId",
                table: "AttributeMeasurements",
                column: "ChemicalId",
                principalTable: "Chemicals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VariableMeasurements_Chemicals_ChemicalId",
                table: "VariableMeasurements",
                column: "ChemicalId",
                principalTable: "Chemicals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributeMeasurements_Chemicals_ChemicalId",
                table: "AttributeMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableMeasurements_Chemicals_ChemicalId",
                table: "VariableMeasurements");

            migrationBuilder.DropTable(
                name: "Chemicals");

            migrationBuilder.DropTable(
                name: "MesSyncMessages");

            migrationBuilder.DropIndex(
                name: "IX_VariableMeasurements_ChemicalId",
                table: "VariableMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_AttributeMeasurements_ChemicalId",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "ChemicalId",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "SideCode",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "ChemicalId",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "SideCode",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "AttributeMeasurementId",
                table: "AlertEvents");

            migrationBuilder.DropColumn(
                name: "UploadBatchId",
                table: "AlertEvents");

            migrationBuilder.DropColumn(
                name: "VariableMeasurementId",
                table: "AlertEvents");

            migrationBuilder.RenameColumn(
                name: "ProcessId",
                table: "AlertEvents",
                newName: "StationId");

            migrationBuilder.RenameColumn(
                name: "PartId",
                table: "AlertEvents",
                newName: "ProductId");

            migrationBuilder.RenameColumn(
                name: "CharacteristicId",
                table: "AlertEvents",
                newName: "InspectionItemId");

            migrationBuilder.AddColumn<int>(
                name: "BatchId",
                table: "AlertEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MeasurementValueId",
                table: "AlertEvents",
                type: "int",
                nullable: true);
        }
    }
}
