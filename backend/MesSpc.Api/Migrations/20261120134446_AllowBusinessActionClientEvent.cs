using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AllowBusinessActionClientEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AuditEvents_ClientEvent",
                table: "AuditEvents");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuditEvents_ClientEvent",
                table: "AuditEvents",
                sql: "[ClientEventId] IS NULL OR ([EventCode] IN ('navigation.page.enter','interaction.business_action') AND [ActorType]='User' AND [SessionIdHash] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AuditEvents_ClientEvent",
                table: "AuditEvents");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuditEvents_ClientEvent",
                table: "AuditEvents",
                sql: "[ClientEventId] IS NULL OR ([EventCode]='navigation.page.enter' AND [ActorType]='User' AND [SessionIdHash] IS NOT NULL)");
        }
    }
}
