using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MesSpc.Api.Infrastructure.Data;

namespace MesSpc.Api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260908095000_AddOperatorPagePermissions")]
public partial class AddOperatorPagePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        name: "PagePermissionsJson", table: "Operators", type: "nvarchar(max)", nullable: true);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "PagePermissionsJson", table: "Operators");
}
