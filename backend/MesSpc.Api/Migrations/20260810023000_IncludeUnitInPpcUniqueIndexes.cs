using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260810023000_IncludeUnitInPpcUniqueIndexes")]
public partial class IncludeUnitInPpcUniqueIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PartProcessCharacteristics_ControlScope_ProcessId_MachineId_TankId_SlotId_CharacteristicId",
            table: "PartProcessCharacteristics");
        migrationBuilder.DropIndex(
            name: "IX_PartProcessCharacteristics_ControlScope_PartId_ProcessId_MachineId_TankId_SlotId_CharacteristicId",
            table: "PartProcessCharacteristics");

        migrationBuilder.CreateIndex(
            name: "IX_PartProcessCharacteristics_ControlScope_ProcessId_MachineId_TankId_SlotId_CharacteristicId_Unit",
            table: "PartProcessCharacteristics",
            columns: new[] { "ControlScope", "ProcessId", "MachineId", "TankId", "SlotId", "CharacteristicId", "Unit" },
            unique: true,
            filter: "[PartId] IS NULL");
        migrationBuilder.CreateIndex(
            name: "IX_PartProcessCharacteristics_ControlScope_PartId_ProcessId_MachineId_TankId_SlotId_CharacteristicId_Unit",
            table: "PartProcessCharacteristics",
            columns: new[] { "ControlScope", "PartId", "ProcessId", "MachineId", "TankId", "SlotId", "CharacteristicId", "Unit" },
            unique: true,
            filter: "[PartId] IS NOT NULL AND [MachineId] IS NOT NULL AND [TankId] IS NOT NULL AND [SlotId] IS NOT NULL AND [Unit] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PartProcessCharacteristics_ControlScope_ProcessId_MachineId_TankId_SlotId_CharacteristicId_Unit",
            table: "PartProcessCharacteristics");
        migrationBuilder.DropIndex(
            name: "IX_PartProcessCharacteristics_ControlScope_PartId_ProcessId_MachineId_TankId_SlotId_CharacteristicId_Unit",
            table: "PartProcessCharacteristics");

        migrationBuilder.CreateIndex(
            name: "IX_PartProcessCharacteristics_ControlScope_ProcessId_MachineId_TankId_SlotId_CharacteristicId",
            table: "PartProcessCharacteristics",
            columns: new[] { "ControlScope", "ProcessId", "MachineId", "TankId", "SlotId", "CharacteristicId" },
            unique: true,
            filter: "[PartId] IS NULL");
        migrationBuilder.CreateIndex(
            name: "IX_PartProcessCharacteristics_ControlScope_PartId_ProcessId_MachineId_TankId_SlotId_CharacteristicId",
            table: "PartProcessCharacteristics",
            columns: new[] { "ControlScope", "PartId", "ProcessId", "MachineId", "TankId", "SlotId", "CharacteristicId" },
            unique: true,
            filter: "[PartId] IS NOT NULL AND [MachineId] IS NOT NULL AND [TankId] IS NOT NULL AND [SlotId] IS NOT NULL");
    }
}
