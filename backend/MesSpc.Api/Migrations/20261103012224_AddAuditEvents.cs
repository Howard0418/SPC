using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, comment: "Audit v1: Id. Append-only; no raw request or secret values.")
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Audit v1: EventId. Append-only; no raw request or secret values."),
                    ClientEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Audit v1: ClientEventId. Append-only; no raw request or secret values."),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false, comment: "Audit v1: SchemaVersion. Append-only; no raw request or secret values."),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Audit v1: OccurredAtUtc. Append-only; no raw request or secret values."),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Audit v1: ReceivedAtUtc. Append-only; no raw request or secret values."),
                    Environment = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "Audit v1: Environment. Append-only; no raw request or secret values."),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "Audit v1: Category. Append-only; no raw request or secret values."),
                    EventCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Audit v1: EventCode. Append-only; no raw request or secret values."),
                    Outcome = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "Audit v1: Outcome. Append-only; no raw request or secret values."),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Audit v1: ReasonCode. Append-only; no raw request or secret values."),
                    ActorType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "Audit v1: ActorType. Append-only; no raw request or secret values."),
                    ActorAssurance = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "Audit v1: ActorAssurance. Append-only; no raw request or secret values."),
                    ActorOperatorId = table.Column<int>(type: "int", nullable: true, comment: "Audit v1: ActorOperatorId. Append-only; no raw request or secret values."),
                    ActorAccountSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Audit v1: ActorAccountSnapshot. Append-only; no raw request or secret values."),
                    ActorOperatorCodeSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Audit v1: ActorOperatorCodeSnapshot. Append-only; no raw request or secret values."),
                    ActorDisplayNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Audit v1: ActorDisplayNameSnapshot. Append-only; no raw request or secret values."),
                    ActorRoleSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Audit v1: ActorRoleSnapshot. Append-only; no raw request or secret values."),
                    ActorServiceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Audit v1: ActorServiceCode. Append-only; no raw request or secret values."),
                    AttemptedAccountHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "Audit v1: AttemptedAccountHash. Append-only; no raw request or secret values."),
                    SessionIdHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "Audit v1: SessionIdHash. Append-only; no raw request or secret values."),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Audit v1: CorrelationId. Append-only; no raw request or secret values."),
                    TraceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Audit v1: TraceId. Append-only; no raw request or secret values."),
                    PageKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Audit v1: PageKey. Append-only; no raw request or secret values."),
                    RouteTemplate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Audit v1: RouteTemplate. Append-only; no raw request or secret values."),
                    TargetType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Audit v1: TargetType. Append-only; no raw request or secret values."),
                    TargetId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Audit v1: TargetId. Append-only; no raw request or secret values."),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, comment: "Audit v1: HttpMethod. Append-only; no raw request or secret values."),
                    HttpStatus = table.Column<int>(type: "int", nullable: true, comment: "Audit v1: HttpStatus. Append-only; no raw request or secret values."),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, comment: "Audit v1: Source. Append-only; no raw request or secret values."),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false, comment: "Audit v1: DetailsJson. Append-only; no raw request or secret values."),
                    DeduplicationHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, comment: "Audit v1: DeduplicationHash. Append-only; no raw request or secret values.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.CheckConstraint("CK_AuditEvents_Actor", "([ActorType]='User' AND [ActorOperatorId] IS NOT NULL AND [ActorOperatorId]>0 AND [ActorAssurance] IN ('Jwt','PortalAssertion')) OR ([ActorType]='Anonymous' AND [ActorOperatorId] IS NULL AND [ActorAssurance]='None' AND [ActorAccountSnapshot] IS NULL AND [ActorOperatorCodeSnapshot] IS NULL AND [ActorDisplayNameSnapshot] IS NULL AND [ActorRoleSnapshot] IS NULL AND [ActorServiceCode] IS NULL) OR ([ActorType]='Service' AND [ActorOperatorId] IS NULL AND [ActorAssurance]='Service' AND [ActorServiceCode] IS NOT NULL)");
                    table.CheckConstraint("CK_AuditEvents_ClientEvent", "[ClientEventId] IS NULL OR ([EventCode]='navigation.page.enter' AND [ActorType]='User' AND [SessionIdHash] IS NOT NULL)");
                    table.CheckConstraint("CK_AuditEvents_DetailsJson", "ISJSON([DetailsJson])=1 AND DATALENGTH([DetailsJson])<=8192");
                    table.CheckConstraint("CK_AuditEvents_Environment", "[Environment] IN ('Test','Production')");
                    table.CheckConstraint("CK_AuditEvents_HttpStatus", "[HttpStatus] IS NULL OR [HttpStatus] BETWEEN 100 AND 599");
                    table.CheckConstraint("CK_AuditEvents_Outcome", "[Outcome] IN ('Succeeded','Denied','Failed')");
                    table.CheckConstraint("CK_AuditEvents_SchemaVersion", "[SchemaVersion] = 1");
                },
                comment: "SPC append-only activity events. Runtime must not update or delete evidence.");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Environment_ActorOperatorId_OccurredAtUtc",
                table: "AuditEvents",
                columns: new[] { "Environment", "ActorOperatorId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Environment_CorrelationId",
                table: "AuditEvents",
                columns: new[] { "Environment", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Environment_EventCode_OccurredAtUtc",
                table: "AuditEvents",
                columns: new[] { "Environment", "EventCode", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Environment_OccurredAtUtc_Id",
                table: "AuditEvents",
                columns: new[] { "Environment", "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Environment_TargetType_TargetId_OccurredAtUtc",
                table: "AuditEvents",
                columns: new[] { "Environment", "TargetType", "TargetId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_EventId",
                table: "AuditEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AuditEvents_ClientRetry",
                table: "AuditEvents",
                columns: new[] { "Environment", "ActorOperatorId", "SessionIdHash", "ClientEventId" },
                unique: true,
                filter: "[ClientEventId] IS NOT NULL");

            if (ActiveProvider.Contains("SqlServer"))
            {
                migrationBuilder.Sql("IF DATABASE_PRINCIPAL_ID(N'SpcAuditWriter') IS NULL CREATE ROLE SpcAuditWriter; IF DATABASE_PRINCIPAL_ID(N'SpcAuditMaintenance') IS NULL CREATE ROLE SpcAuditMaintenance;");
                migrationBuilder.Sql("GRANT SELECT, INSERT ON dbo.AuditEvents TO SpcAuditWriter; DENY UPDATE, DELETE, ALTER ON dbo.AuditEvents TO SpcAuditWriter;");
                migrationBuilder.Sql("CREATE TRIGGER dbo.TR_AuditEvents_NoUpdate ON dbo.AuditEvents INSTEAD OF UPDATE AS BEGIN SET NOCOUNT ON; THROW 51301, 'Audit events are append-only', 1; END;");
                migrationBuilder.Sql("CREATE TRIGGER dbo.TR_AuditEvents_ControlledDelete ON dbo.AuditEvents INSTEAD OF DELETE AS BEGIN SET NOCOUNT ON; IF IS_ROLEMEMBER(N'SpcAuditMaintenance')<>1 BEGIN THROW 51302, 'Audit maintenance role required', 1; END; DELETE a FROM dbo.AuditEvents a INNER JOIN deleted d ON a.Id=d.Id; END;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("SqlServer"))
            {
                migrationBuilder.Sql("IF EXISTS(SELECT 1 FROM dbo.AuditEvents) THROW 51303, 'Cannot remove audit evidence; restore API without dropping this table', 1;");
                migrationBuilder.Sql("DROP TRIGGER dbo.TR_AuditEvents_NoUpdate; DROP TRIGGER dbo.TR_AuditEvents_ControlledDelete;");
            }
            migrationBuilder.DropTable(
                name: "AuditEvents");
        }
    }
}
