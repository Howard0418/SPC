using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddControlScopeToPartProcessCharacteristics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_PartId_ProcessId_CharacteristicId",
                table: "PartProcessCharacteristics");

            migrationBuilder.AlterColumn<int>(
                name: "PartId",
                table: "PartProcessCharacteristics",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "ControlScope",
                table: "PartProcessCharacteristics",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "PRODUCT");

            migrationBuilder.Sql("""
                UPDATE ppc
                SET ControlScope = CASE WHEN p.PartNo = 'COMMON' THEN 'PROCESS' ELSE 'PRODUCT' END,
                    PartId = CASE WHEN p.PartNo = 'COMMON' THEN NULL ELSE ppc.PartId END
                FROM PartProcessCharacteristics ppc
                LEFT JOIN Parts p ON p.Id = ppc.PartId
                """);

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

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_PartId",
                table: "PartProcessCharacteristics",
                column: "PartId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_PartId_ProcessId_CharacteristicId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_ControlScope_ProcessId_CharacteristicId",
                table: "PartProcessCharacteristics");

            migrationBuilder.DropIndex(
                name: "IX_PartProcessCharacteristics_PartId",
                table: "PartProcessCharacteristics");

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM Parts WHERE PartNo = 'COMMON')
                BEGIN
                    INSERT INTO Parts (PartNo, PartName, IsEnabled, CreatedAt, IsDeleted)
                    VALUES ('COMMON', N'共用產品/線路', 1, SYSUTCDATETIME(), 0)
                END

                UPDATE ppc
                SET PartId = p.Id
                FROM PartProcessCharacteristics ppc
                CROSS JOIN (SELECT TOP 1 Id FROM Parts WHERE PartNo = 'COMMON') p
                WHERE ppc.PartId IS NULL
                """);

            migrationBuilder.DropColumn(
                name: "ControlScope",
                table: "PartProcessCharacteristics");

            migrationBuilder.AlterColumn<int>(
                name: "PartId",
                table: "PartProcessCharacteristics",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessCharacteristics_PartId_ProcessId_CharacteristicId",
                table: "PartProcessCharacteristics",
                columns: new[] { "PartId", "ProcessId", "CharacteristicId" },
                unique: true);
        }
    }
}
