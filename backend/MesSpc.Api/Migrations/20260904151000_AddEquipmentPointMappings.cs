using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MesSpc.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260904151000_AddEquipmentPointMappings")]
public class AddEquipmentPointMappings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "EquipmentPointMappings", columns: table => new { Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"), SourceId = table.Column<string>(maxLength: 64, nullable: false), EquipmentId = table.Column<string>(maxLength: 128, nullable: false), ChannelId = table.Column<string>(maxLength: 128, nullable: false), DisplayName = table.Column<string>(maxLength: 200, nullable: true), Unit = table.Column<string>(maxLength: 30, nullable: true), IsEnabled = table.Column<bool>(nullable: false), CreatedAt = table.Column<DateTime>(nullable: false), CreatedBy = table.Column<string>(nullable: true), UpdatedAt = table.Column<DateTime>(nullable: true), UpdatedBy = table.Column<string>(nullable: true), IsDeleted = table.Column<bool>(nullable: false), RowVersion = table.Column<byte[]>(rowVersion: true, nullable: true) }, constraints: table => table.PrimaryKey("PK_EquipmentPointMappings", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_EquipmentPointMappings_SourceId_EquipmentId_ChannelId", table: "EquipmentPointMappings", columns: new[] { "SourceId", "EquipmentId", "ChannelId" }, unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "EquipmentPointMappings");
}
