using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260807000100_StandardizeControlScopeCodes")]
public partial class StandardizeControlScopeCodes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE Processes SET ControlScope = CASE UPPER(LTRIM(RTRIM(ControlScope)))
                WHEN 'CHEMICAL' THEN 'CHEM' WHEN 'PROD' THEN 'PRODUCT' WHEN 'PROC' THEN 'PROCESS'
                ELSE UPPER(LTRIM(RTRIM(ControlScope))) END;
            UPDATE QualityCharacteristics SET ControlScope = CASE UPPER(LTRIM(RTRIM(ControlScope)))
                WHEN 'CHEMICAL' THEN 'CHEM' WHEN 'PROD' THEN 'PRODUCT' WHEN 'PROC' THEN 'PROCESS'
                ELSE UPPER(LTRIM(RTRIM(ControlScope))) END;
            UPDATE PartProcessCharacteristics SET ControlScope = CASE UPPER(LTRIM(RTRIM(ControlScope)))
                WHEN 'CHEMICAL' THEN 'CHEM' WHEN 'PROD' THEN 'PRODUCT' WHEN 'PROC' THEN 'PROCESS'
                ELSE UPPER(LTRIM(RTRIM(ControlScope))) END;
            UPDATE ControlChartGroups SET BusinessScopeCode = CASE UPPER(LTRIM(RTRIM(BusinessScopeCode)))
                WHEN 'CHEMICAL' THEN 'CHEM' WHEN 'PROD' THEN 'PRODUCT' WHEN 'PROC' THEN 'PROCESS'
                ELSE UPPER(LTRIM(RTRIM(BusinessScopeCode))) END;

            ALTER TABLE Processes ADD CONSTRAINT CK_Processes_ControlScope_Canonical
                CHECK (LEN(LTRIM(RTRIM(ControlScope))) > 0 AND ControlScope = UPPER(LTRIM(RTRIM(ControlScope))) AND ControlScope NOT IN ('CHEMICAL','PROD','PROC'));
            ALTER TABLE QualityCharacteristics ADD CONSTRAINT CK_QualityCharacteristics_ControlScope_Canonical
                CHECK (LEN(LTRIM(RTRIM(ControlScope))) > 0 AND ControlScope = UPPER(LTRIM(RTRIM(ControlScope))) AND ControlScope NOT IN ('CHEMICAL','PROD','PROC'));
            ALTER TABLE PartProcessCharacteristics ADD CONSTRAINT CK_PartProcessCharacteristics_ControlScope_Canonical
                CHECK (LEN(LTRIM(RTRIM(ControlScope))) > 0 AND ControlScope = UPPER(LTRIM(RTRIM(ControlScope))) AND ControlScope NOT IN ('CHEMICAL','PROD','PROC'));
            ALTER TABLE ControlChartGroups ADD CONSTRAINT CK_ControlChartGroups_BusinessScopeCode_Canonical
                CHECK (LEN(LTRIM(RTRIM(BusinessScopeCode))) > 0 AND BusinessScopeCode = UPPER(LTRIM(RTRIM(BusinessScopeCode))) AND BusinessScopeCode NOT IN ('CHEMICAL','PROD','PROC'));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE Processes DROP CONSTRAINT CK_Processes_ControlScope_Canonical;
            ALTER TABLE QualityCharacteristics DROP CONSTRAINT CK_QualityCharacteristics_ControlScope_Canonical;
            ALTER TABLE PartProcessCharacteristics DROP CONSTRAINT CK_PartProcessCharacteristics_ControlScope_Canonical;
            ALTER TABLE ControlChartGroups DROP CONSTRAINT CK_ControlChartGroups_BusinessScopeCode_Canonical;
            """);
    }
}
