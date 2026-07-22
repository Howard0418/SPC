using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations;

public partial class AddCharacteristicControlScope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ControlScope",
            table: "QualityCharacteristics",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "PRODUCT");

        migrationBuilder.Sql("""
            UPDATE qc
            SET ControlScope = COALESCE(scopeInfo.ControlScope, 'PRODUCT')
            FROM QualityCharacteristics qc
            OUTER APPLY (
                SELECT TOP (1) ppc.ControlScope
                FROM PartProcessCharacteristics ppc
                WHERE ppc.CharacteristicId = qc.Id
                GROUP BY ppc.ControlScope
                ORDER BY COUNT(*) DESC, ppc.ControlScope
            ) scopeInfo;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ControlScope", table: "QualityCharacteristics");
    }
}
