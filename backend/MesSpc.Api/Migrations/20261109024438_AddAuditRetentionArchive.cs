using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditRetentionArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditArchiveBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RangeFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RangeToUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordCount = table.Column<int>(type: "int", nullable: false),
                    Checksum = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EncryptionKeyId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditArchiveBatches", x => x.Id);
                },
                comment: "Verified audit archive batches; maintenance identity only.");

            migrationBuilder.CreateTable(
                name: "AuditLegalHolds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DateFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateToUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PageKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedByOperatorId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReleasedByOperatorId = table.Column<int>(type: "int", nullable: true),
                    ReleasedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLegalHolds", x => x.Id);
                },
                comment: "Legal holds override audit retention and purge.");

            migrationBuilder.CreateTable(
                name: "AuditEventArchives",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalEventId = table.Column<long>(type: "bigint", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArchiveBatchId = table.Column<long>(type: "bigint", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EventCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PageKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Nonce = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Ciphertext = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Tag = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEventArchives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEventArchives_AuditArchiveBatches_ArchiveBatchId",
                        column: x => x.ArchiveBatchId,
                        principalTable: "AuditArchiveBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Encrypted audit event archive; controlled maintenance deletion only.");

            migrationBuilder.CreateIndex(
                name: "IX_AuditArchiveBatches_BatchId",
                table: "AuditArchiveBatches",
                column: "BatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEventArchives_ArchiveBatchId",
                table: "AuditEventArchives",
                column: "ArchiveBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEventArchives_Environment_OccurredAtUtc_Id",
                table: "AuditEventArchives",
                columns: new[] { "Environment", "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEventArchives_EventId",
                table: "AuditEventArchives",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEventArchives_OriginalEventId",
                table: "AuditEventArchives",
                column: "OriginalEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLegalHolds_Environment_ReleasedAtUtc_DateFromUtc_DateToUtc",
                table: "AuditLegalHolds",
                columns: new[] { "Environment", "ReleasedAtUtc", "DateFromUtc", "DateToUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLegalHolds_HoldId",
                table: "AuditLegalHolds",
                column: "HoldId",
                unique: true);

            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_AuditEventArchives_Immutable] ON [AuditEventArchives]
                AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted) THROW 51031, 'Audit archives are immutable.', 1;
                    IF IS_ROLEMEMBER('SpcAuditMaintenance') <> 1 THROW 51032, 'Audit archive deletion requires maintenance role.', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_AuditArchiveBatches_DeleteGuard] ON [AuditArchiveBatches]
                INSTEAD OF DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF IS_ROLEMEMBER('SpcAuditMaintenance') <> 1 THROW 51033, 'Audit batch deletion requires maintenance role.', 1;
                    IF EXISTS (SELECT 1 FROM [AuditEventArchives] a JOIN deleted d ON d.Id=a.ArchiveBatchId)
                        THROW 51034, 'Audit batch has archive evidence.', 1;
                    DELETE b FROM [AuditArchiveBatches] b JOIN deleted d ON d.Id=b.Id;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [AuditEventArchives]) OR EXISTS (SELECT 1 FROM [AuditLegalHolds])
                    THROW 51035, 'Retention rollback refused because archive or legal-hold evidence exists.', 1;
                DROP TRIGGER IF EXISTS [TR_AuditEventArchives_Immutable];
                DROP TRIGGER IF EXISTS [TR_AuditArchiveBatches_DeleteGuard];
                """);
            migrationBuilder.DropTable(
                name: "AuditEventArchives");

            migrationBuilder.DropTable(
                name: "AuditLegalHolds");

            migrationBuilder.DropTable(
                name: "AuditArchiveBatches");
        }
    }
}
