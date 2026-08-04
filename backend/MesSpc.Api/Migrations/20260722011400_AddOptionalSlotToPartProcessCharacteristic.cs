using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOptionalSlotToPartProcessCharacteristic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_PartId_ProcessId_MachineId_TankId_CharacteristicId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_ProcessId_MachineId_TankId_CharacteristicId",
                table: "PartProcessCharacteristics");

            migrationBuilder.AddColumn<int>(
                name: "SlotId",
                table: "PartProcessCharacteristics",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_PartId_ProcessId_MachineId_TankId_SlotId_CharacteristicId",
                table: "PartProcessCharacteristics",
                columns: new[] { "ControlScope", "PartId", "ProcessId", "MachineId", "TankId", "SlotId", "CharacteristicId" },
                unique: true,
                filter: "[PartId] IS NOT NULL AND [MachineId] IS NOT NULL AND [TankId] IS NOT NULL AND [SlotId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_ProcessId_MachineId_TankId_SlotId_CharacteristicId",
                table: "PartProcessCharacteristics",
                columns: new[] { "ControlScope", "ProcessId", "MachineId", "TankId", "SlotId", "CharacteristicId" },
                unique: true,
                filter: "[PartId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_SlotId",
                table: "PartProcessCharacteristics",
                column: "SlotId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartProcessCharacteristics_Slots_SlotId",
                table: "PartProcessCharacteristics",
                column: "SlotId",
                principalTable: "Slots",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartProcessCharacteristics_Slots_SlotId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_PartId_ProcessId_MachineId_TankId_SlotId_CharacteristicId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_ProcessId_MachineId_TankId_SlotId_CharacteristicId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_SlotId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropColumn(
                name: "SlotId",
                table: "PartProcessCharacteristics");

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
        }
    }
}
