using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_LotSplitTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentLotNo",
                table: "VariableMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubLotNo",
                table: "VariableMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkOrderNo",
                table: "VariableMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentLotNo",
                table: "AttributeMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubLotNo",
                table: "AttributeMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkOrderNo",
                table: "AttributeMeasurements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LotMasters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubLotNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentLotId = table.Column<long>(type: "bigint", nullable: true),
                    WorkOrderId = table.Column<int>(type: "int", nullable: true),
                    PartId = table.Column<int>(type: "int", nullable: true),
                    CurrentQty = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotMasters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LotMasters_LotMasters_ParentLotId",
                        column: x => x.ParentLotId,
                        principalTable: "LotMasters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LotSplitHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceLotId = table.Column<long>(type: "bigint", nullable: false),
                    TargetLotId = table.Column<long>(type: "bigint", nullable: false),
                    SplitTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SplitQty = table.Column<int>(type: "int", nullable: false),
                    SplitReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SplitOperator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotSplitHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LotSplitHistories_LotMasters_SourceLotId",
                        column: x => x.SourceLotId,
                        principalTable: "LotMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LotSplitHistories_LotMasters_TargetLotId",
                        column: x => x.TargetLotId,
                        principalTable: "LotMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LotMasters_LotNo",
                table: "LotMasters",
                column: "LotNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LotMasters_ParentLotId",
                table: "LotMasters",
                column: "ParentLotId");

            migrationBuilder.CreateIndex(
                name: "IX_LotSplitHistories_SourceLotId",
                table: "LotSplitHistories",
                column: "SourceLotId");

            migrationBuilder.CreateIndex(
                name: "IX_LotSplitHistories_TargetLotId",
                table: "LotSplitHistories",
                column: "TargetLotId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LotSplitHistories");

            migrationBuilder.DropTable(
                name: "LotMasters");

            migrationBuilder.DropColumn(
                name: "ParentLotNo",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "SubLotNo",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "WorkOrderNo",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "ParentLotNo",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "SubLotNo",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "WorkOrderNo",
                table: "AttributeMeasurements");
        }
    }
}
