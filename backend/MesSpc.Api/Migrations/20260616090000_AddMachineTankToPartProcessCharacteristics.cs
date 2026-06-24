using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    public partial class AddMachineTankToPartProcessCharacteristics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_PartId_ProcessId_CharacteristicId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_ProcessId_CharacteristicId",
                table: "PartProcessCharacteristics");

            migrationBuilder.AddColumn<int>(
                name: "MachineId",
                table: "PartProcessCharacteristics",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TankId",
                table: "PartProcessCharacteristics",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_MachineId",
                table: "PartProcessCharacteristics",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_TankId",
                table: "PartProcessCharacteristics",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_PartId_ProcessId_MachineId_TankId_CharacteristicId",
                table: "PartProcessCharacteristics",
                columns: new[] { "ControlScope", "PartId", "ProcessId", "MachineId", "TankId", "CharacteristicId" },
                unique: true,
                filter: "[PartId] IS NOT NULL AND [MachineId] IS NOT NULL AND [TankId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_ProcessId_MachineId_TankId_CharacteristicId",
                table: "PartProcessCharacteristics",
                columns: new[] { "ControlScope", "ProcessId", "MachineId", "TankId", "CharacteristicId" },
                unique: true,
                filter: "[PartId] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PartProcessCharacteristics_Machines_MachineId",
                table: "PartProcessCharacteristics",
                column: "MachineId",
                principalTable: "Machines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PartProcessCharacteristics_Tanks_TankId",
                table: "PartProcessCharacteristics",
                column: "TankId",
                principalTable: "Tanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartProcessCharacteristics_Machines_MachineId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropForeignKey(
                name: "FK_PartProcessCharacteristics_Tanks_TankId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_MachineId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_TankId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_PartId_ProcessId_MachineId_TankId_CharacteristicId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_ProcessId_MachineId_TankId_CharacteristicId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropColumn(
                name: "MachineId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropColumn(
                name: "TankId",
                table: "PartProcessCharacteristics");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_PartId_ProcessId_CharacteristicId",
                table: "PartProcessCharacteristics",
                columns: new[] { "ControlScope", "PartId", "ProcessId", "CharacteristicId" },
                unique: true,
                filter: "[PartId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_ProcessId_CharacteristicId",
                table: "PartProcessCharacteristics",
                columns: new[] { "ControlScope", "ProcessId", "CharacteristicId" },
                unique: true,
                filter: "[PartId] IS NULL");
        }
    }
}
