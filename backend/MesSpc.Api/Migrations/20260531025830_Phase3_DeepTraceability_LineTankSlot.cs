using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_DeepTraceability_LineTankSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LineId",
                table: "VariableMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlotId",
                table: "VariableMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TankId",
                table: "VariableMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LineId",
                table: "AttributeMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SlotId",
                table: "AttributeMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TankId",
                table: "AttributeMeasurements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Tanks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LineId = table.Column<int>(type: "int", nullable: false),
                    TankCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TankName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Tanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tanks_ProductionLines_LineId",
                        column: x => x.LineId,
                        principalTable: "ProductionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Slots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TankId = table.Column<int>(type: "int", nullable: false),
                    SlotCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SlotName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SequenceNo = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Slots_Tanks_TankId",
                        column: x => x.TankId,
                        principalTable: "Tanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LotSlotHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotId = table.Column<long>(type: "bigint", nullable: false),
                    SlotId = table.Column<int>(type: "int", nullable: false),
                    EntryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Operator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotSlotHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LotSlotHistories_LotMasters_LotId",
                        column: x => x.LotId,
                        principalTable: "LotMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LotSlotHistories_Slots_SlotId",
                        column: x => x.SlotId,
                        principalTable: "Slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlotParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SlotId = table.Column<int>(type: "int", nullable: false),
                    ParameterCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParameterName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetValue = table.Column<double>(type: "float", nullable: true),
                    Usl = table.Column<double>(type: "float", nullable: true),
                    Lsl = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlotParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlotParameters_Slots_SlotId",
                        column: x => x.SlotId,
                        principalTable: "Slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_LineId",
                table: "VariableMeasurements",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_SlotId",
                table: "VariableMeasurements",
                column: "SlotId");

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_TankId",
                table: "VariableMeasurements",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeMeasurements_LineId",
                table: "AttributeMeasurements",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeMeasurements_SlotId",
                table: "AttributeMeasurements",
                column: "SlotId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeMeasurements_TankId",
                table: "AttributeMeasurements",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_LotSlotHistories_LotId",
                table: "LotSlotHistories",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_LotSlotHistories_SlotId",
                table: "LotSlotHistories",
                column: "SlotId");

            migrationBuilder.CreateIndex(
                name: "IX_SlotParameters_SlotId",
                table: "SlotParameters",
                column: "SlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Slots_SlotCode",
                table: "Slots",
                column: "SlotCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Slots_TankId",
                table: "Slots",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_Tanks_LineId",
                table: "Tanks",
                column: "LineId");

            migrationBuilder.CreateIndex(
                name: "IX_Tanks_TankCode",
                table: "Tanks",
                column: "TankCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeMeasurements_ProductionLines_LineId",
                table: "AttributeMeasurements",
                column: "LineId",
                principalTable: "ProductionLines",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeMeasurements_Slots_SlotId",
                table: "AttributeMeasurements",
                column: "SlotId",
                principalTable: "Slots",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeMeasurements_Tanks_TankId",
                table: "AttributeMeasurements",
                column: "TankId",
                principalTable: "Tanks",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VariableMeasurements_ProductionLines_LineId",
                table: "VariableMeasurements",
                column: "LineId",
                principalTable: "ProductionLines",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VariableMeasurements_Slots_SlotId",
                table: "VariableMeasurements",
                column: "SlotId",
                principalTable: "Slots",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VariableMeasurements_Tanks_TankId",
                table: "VariableMeasurements",
                column: "TankId",
                principalTable: "Tanks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributeMeasurements_ProductionLines_LineId",
                table: "AttributeMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_AttributeMeasurements_Slots_SlotId",
                table: "AttributeMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_AttributeMeasurements_Tanks_TankId",
                table: "AttributeMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableMeasurements_ProductionLines_LineId",
                table: "VariableMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableMeasurements_Slots_SlotId",
                table: "VariableMeasurements");

            migrationBuilder.DropForeignKey(
                name: "FK_VariableMeasurements_Tanks_TankId",
                table: "VariableMeasurements");

            migrationBuilder.DropTable(
                name: "LotSlotHistories");

            migrationBuilder.DropTable(
                name: "SlotParameters");

            migrationBuilder.DropTable(
                name: "Slots");

            migrationBuilder.DropTable(
                name: "Tanks");

            migrationBuilder.DropIndex(
                name: "IX_VariableMeasurements_LineId",
                table: "VariableMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_VariableMeasurements_SlotId",
                table: "VariableMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_VariableMeasurements_TankId",
                table: "VariableMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_AttributeMeasurements_LineId",
                table: "AttributeMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_AttributeMeasurements_SlotId",
                table: "AttributeMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_AttributeMeasurements_TankId",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "LineId",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "SlotId",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "TankId",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "LineId",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "SlotId",
                table: "AttributeMeasurements");

            migrationBuilder.DropColumn(
                name: "TankId",
                table: "AttributeMeasurements");
        }
    }
}
