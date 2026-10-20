using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChemicalFormulaTraceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChemicalReceiptId",
                table: "VariableMeasurements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChemicalSourceKeyHash",
                table: "VariableMeasurements",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ChemicalAnalysisFormulaVersions_Id_PartProcessCharacteristicId",
                table: "ChemicalAnalysisFormulaVersions",
                columns: new[] { "Id", "PartProcessCharacteristicId" });

            migrationBuilder.CreateTable(
                name: "ChemicalFormulaScheduleRevisions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartProcessCharacteristicId = table.Column<int>(type: "int", nullable: false),
                    RevisionNo = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Actor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalFormulaScheduleRevisions", x => x.Id);
                    table.UniqueConstraint("AK_ChemicalFormulaScheduleRevisions_Id_PartProcessCharacteristicId", x => new { x.Id, x.PartProcessCharacteristicId });
                    table.CheckConstraint("CK_ChemicalSchedule_Revision", "[RevisionNo] > 0");
                    table.ForeignKey(
                        name: "FK_ChemicalFormulaScheduleRevisions_PartProcessCharacteristics_PartProcessCharacteristicId",
                        column: x => x.PartProcessCharacteristicId,
                        principalTable: "PartProcessCharacteristics",
                        principalColumn: "Id");
                },
                comment: "Immutable published formula schedule revision sets.");

            migrationBuilder.CreateTable(
                name: "ChemicalFormulaEffectivities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartProcessCharacteristicId = table.Column<int>(type: "int", nullable: false),
                    ScheduleRevisionId = table.Column<long>(type: "bigint", nullable: false),
                    FormulaVersionId = table.Column<long>(type: "bigint", nullable: false),
                    FTableHistoryId = table.Column<long>(type: "bigint", nullable: true),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveUntilUtcExclusive = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalFormulaEffectivities", x => x.Id);
                    table.UniqueConstraint("AK_ChemicalFormulaEffectivities_Id_PartProcessCharacteristicId", x => new { x.Id, x.PartProcessCharacteristicId });
                    table.CheckConstraint("CK_ChemicalEffectivity_Range", "[EffectiveUntilUtcExclusive] IS NULL OR [EffectiveFromUtc] < [EffectiveUntilUtcExclusive]");
                    table.ForeignKey(
                        name: "FK_ChemicalFormulaEffectivities_ChemicalAnalysisFormulaVersions_FormulaVersionId_PartProcessCharacteristicId",
                        columns: x => new { x.FormulaVersionId, x.PartProcessCharacteristicId },
                        principalTable: "ChemicalAnalysisFormulaVersions",
                        principalColumns: new[] { "Id", "PartProcessCharacteristicId" });
                    table.ForeignKey(
                        name: "FK_ChemicalFormulaEffectivities_ChemicalFTableVersionHistories_FTableHistoryId",
                        column: x => x.FTableHistoryId,
                        principalTable: "ChemicalFTableVersionHistories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChemicalFormulaEffectivities_ChemicalFormulaScheduleRevisions_ScheduleRevisionId_PartProcessCharacteristicId",
                        columns: x => new { x.ScheduleRevisionId, x.PartProcessCharacteristicId },
                        principalTable: "ChemicalFormulaScheduleRevisions",
                        principalColumns: new[] { "Id", "PartProcessCharacteristicId" });
                },
                comment: "Half-open UTC intervals referencing existing formula and F snapshots.");

            migrationBuilder.CreateTable(
                name: "ChemicalFormulaPolicies",
                columns: table => new
                {
                    PartProcessCharacteristicId = table.Column<int>(type: "int", nullable: false),
                    ActiveScheduleRevisionId = table.Column<long>(type: "bigint", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalFormulaPolicies", x => x.PartProcessCharacteristicId);
                    table.CheckConstraint("CK_ChemicalPolicy_Active", "[IsEnabled] = 0 OR [ActiveScheduleRevisionId] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_ChemicalFormulaPolicies_ChemicalFormulaScheduleRevisions_ActiveScheduleRevisionId_PartProcessCharacteristicId",
                        columns: x => new { x.ActiveScheduleRevisionId, x.PartProcessCharacteristicId },
                        principalTable: "ChemicalFormulaScheduleRevisions",
                        principalColumns: new[] { "Id", "PartProcessCharacteristicId" });
                    table.ForeignKey(
                        name: "FK_ChemicalFormulaPolicies_PartProcessCharacteristics_PartProcessCharacteristicId",
                        column: x => x.PartProcessCharacteristicId,
                        principalTable: "PartProcessCharacteristics",
                        principalColumn: "Id");
                },
                comment: "Per-PPC opt-in; existing PPCs are not enabled or backfilled.");

            migrationBuilder.CreateTable(
                name: "ChemicalCalculationReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartProcessCharacteristicId = table.Column<int>(type: "int", nullable: false),
                    EffectivityId = table.Column<long>(type: "bigint", nullable: false),
                    MeasuredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InputHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceKeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EngineVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrecisionPolicy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalCalculationReceipts", x => x.Id);
                    table.CheckConstraint("CK_ChemicalReceipt_Expiry", "[CreatedAtUtc] < [ExpiresAtUtc]");
                    table.ForeignKey(
                        name: "FK_ChemicalCalculationReceipts_ChemicalFormulaEffectivities_EffectivityId_PartProcessCharacteristicId",
                        columns: x => new { x.EffectivityId, x.PartProcessCharacteristicId },
                        principalTable: "ChemicalFormulaEffectivities",
                        principalColumns: new[] { "Id", "PartProcessCharacteristicId" });
                },
                comment: "Immutable server-prepared evidence; canonical values and F snapshots in payload.");

            migrationBuilder.CreateTable(
                name: "ChemicalCalculationTraces",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VariableMeasurementId = table.Column<long>(type: "bigint", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    RawResultCanonical = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StoredResultCanonical = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalCalculationTraces", x => x.Id);
                    table.CheckConstraint("CK_ChemicalTrace_Phase", "[Phase] IN ('Initial','Recheck') AND [VersionNo] > 0");
                    table.ForeignKey(
                        name: "FK_ChemicalCalculationTraces_ChemicalCalculationReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "ChemicalCalculationReceipts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChemicalCalculationTraces_VariableMeasurements_VariableMeasurementId",
                        column: x => x.VariableMeasurementId,
                        principalTable: "VariableMeasurements",
                        principalColumn: "Id");
                },
                comment: "Append-only phase versions; legacy measurements have no invented trace.");

            migrationBuilder.CreateTable(
                name: "ChemicalStatisticsJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariableMeasurementId = table.Column<long>(type: "bigint", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalStatisticsJobs", x => x.Id);
                    table.CheckConstraint("CK_ChemicalJob_State", "[Attempts] >= 0 AND [Status] IN ('Pending','Processing','Completed','Failed')");
                    table.ForeignKey(
                        name: "FK_ChemicalStatisticsJobs_ChemicalCalculationReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "ChemicalCalculationReceipts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChemicalStatisticsJobs_VariableMeasurements_VariableMeasurementId",
                        column: x => x.VariableMeasurementId,
                        principalTable: "VariableMeasurements",
                        principalColumn: "Id");
                },
                comment: "Transactional statistics outbox, consumer delivered separately.");

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_ChemicalReceiptId",
                table: "VariableMeasurements",
                column: "ChemicalReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId_ChemicalSourceKeyHash",
                table: "VariableMeasurements",
                columns: new[] { "PartProcessCharacteristicId", "ChemicalSourceKeyHash" },
                unique: true,
                filter: "[ChemicalSourceKeyHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalCalculationReceipts_EffectivityId_PartProcessCharacteristicId",
                table: "ChemicalCalculationReceipts",
                columns: new[] { "EffectivityId", "PartProcessCharacteristicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalCalculationReceipts_PartProcessCharacteristicId_SourceKeyHash",
                table: "ChemicalCalculationReceipts",
                columns: new[] { "PartProcessCharacteristicId", "SourceKeyHash" });

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalCalculationTraces_ReceiptId_Phase",
                table: "ChemicalCalculationTraces",
                columns: new[] { "ReceiptId", "Phase" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalCalculationTraces_VariableMeasurementId_Phase_VersionNo",
                table: "ChemicalCalculationTraces",
                columns: new[] { "VariableMeasurementId", "Phase", "VersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFormulaEffectivities_FormulaVersionId_PartProcessCharacteristicId",
                table: "ChemicalFormulaEffectivities",
                columns: new[] { "FormulaVersionId", "PartProcessCharacteristicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFormulaEffectivities_FTableHistoryId",
                table: "ChemicalFormulaEffectivities",
                column: "FTableHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFormulaEffectivities_ScheduleRevisionId_EffectiveFromUtc",
                table: "ChemicalFormulaEffectivities",
                columns: new[] { "ScheduleRevisionId", "EffectiveFromUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFormulaEffectivities_ScheduleRevisionId_PartProcessCharacteristicId",
                table: "ChemicalFormulaEffectivities",
                columns: new[] { "ScheduleRevisionId", "PartProcessCharacteristicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFormulaPolicies_ActiveScheduleRevisionId_PartProcessCharacteristicId",
                table: "ChemicalFormulaPolicies",
                columns: new[] { "ActiveScheduleRevisionId", "PartProcessCharacteristicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFormulaScheduleRevisions_PartProcessCharacteristicId_RevisionNo",
                table: "ChemicalFormulaScheduleRevisions",
                columns: new[] { "PartProcessCharacteristicId", "RevisionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalStatisticsJobs_ReceiptId",
                table: "ChemicalStatisticsJobs",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalStatisticsJobs_Status_NextAttemptAtUtc",
                table: "ChemicalStatisticsJobs",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalStatisticsJobs_VariableMeasurementId_ReceiptId",
                table: "ChemicalStatisticsJobs",
                columns: new[] { "VariableMeasurementId", "ReceiptId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VariableMeasurements_ChemicalCalculationReceipts_ChemicalReceiptId",
                table: "VariableMeasurements",
                column: "ChemicalReceiptId",
                principalTable: "ChemicalCalculationReceipts",
                principalColumn: "Id");
            if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
                foreach (var sql in ChemicalTraceSqlV1.Up) migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
                foreach (var sql in ChemicalTraceSqlV1.Down) migrationBuilder.Sql(sql);
            migrationBuilder.DropForeignKey(
                name: "FK_VariableMeasurements_ChemicalCalculationReceipts_ChemicalReceiptId",
                table: "VariableMeasurements");

            migrationBuilder.DropTable(
                name: "ChemicalCalculationTraces");

            migrationBuilder.DropTable(
                name: "ChemicalFormulaPolicies");

            migrationBuilder.DropTable(
                name: "ChemicalStatisticsJobs");

            migrationBuilder.DropTable(
                name: "ChemicalCalculationReceipts");

            migrationBuilder.DropTable(
                name: "ChemicalFormulaEffectivities");

            migrationBuilder.DropTable(
                name: "ChemicalFormulaScheduleRevisions");

            migrationBuilder.DropIndex(
                name: "IX_VariableMeasurements_ChemicalReceiptId",
                table: "VariableMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_VariableMeasurements_PartProcessCharacteristicId_ChemicalSourceKeyHash",
                table: "VariableMeasurements");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ChemicalAnalysisFormulaVersions_Id_PartProcessCharacteristicId",
                table: "ChemicalAnalysisFormulaVersions");

            migrationBuilder.DropColumn(
                name: "ChemicalReceiptId",
                table: "VariableMeasurements");

            migrationBuilder.DropColumn(
                name: "ChemicalSourceKeyHash",
                table: "VariableMeasurements");
        }
    }
}
