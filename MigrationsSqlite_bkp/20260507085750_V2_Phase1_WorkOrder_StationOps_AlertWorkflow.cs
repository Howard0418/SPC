using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class V2_Phase1_WorkOrder_StationOps_AlertWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LotNo",
                table: "MeasurementBatches",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNo",
                table: "MeasurementBatches",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StationOperationSessionId",
                table: "MeasurementBatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkOrderId",
                table: "MeasurementBatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "AlertEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectiveAction",
                table: "AlertEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleUser",
                table: "AlertEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RootCause",
                table: "AlertEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AlertEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AlertEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StationOperationSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderId = table.Column<int>(type: "int", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SerialNo = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OperatorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StationOperationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkOrderNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    PlannedQty = table.Column<int>(type: "int", nullable: false),
                    ActualQty = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlannedStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementBatches_LotNo",
                table: "MeasurementBatches",
                column: "LotNo");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementBatches_SerialNo",
                table: "MeasurementBatches",
                column: "SerialNo");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementBatches_WorkOrderId",
                table: "MeasurementBatches",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StationOperationSessions_LotNo",
                table: "StationOperationSessions",
                column: "LotNo");

            migrationBuilder.CreateIndex(
                name: "IX_StationOperationSessions_SerialNo",
                table: "StationOperationSessions",
                column: "SerialNo");

            migrationBuilder.CreateIndex(
                name: "IX_StationOperationSessions_WorkOrderId",
                table: "StationOperationSessions",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_WorkOrderNo",
                table: "WorkOrders",
                column: "WorkOrderNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StationOperationSessions");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropIndex(
                name: "IX_MeasurementBatches_LotNo",
                table: "MeasurementBatches");

            migrationBuilder.DropIndex(
                name: "IX_MeasurementBatches_SerialNo",
                table: "MeasurementBatches");

            migrationBuilder.DropIndex(
                name: "IX_MeasurementBatches_WorkOrderId",
                table: "MeasurementBatches");

            migrationBuilder.DropColumn(
                name: "LotNo",
                table: "MeasurementBatches");

            migrationBuilder.DropColumn(
                name: "SerialNo",
                table: "MeasurementBatches");

            migrationBuilder.DropColumn(
                name: "StationOperationSessionId",
                table: "MeasurementBatches");

            migrationBuilder.DropColumn(
                name: "WorkOrderId",
                table: "MeasurementBatches");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "AlertEvents");

            migrationBuilder.DropColumn(
                name: "CorrectiveAction",
                table: "AlertEvents");

            migrationBuilder.DropColumn(
                name: "ResponsibleUser",
                table: "AlertEvents");

            migrationBuilder.DropColumn(
                name: "RootCause",
                table: "AlertEvents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AlertEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AlertEvents");
        }
    }
}
