using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecificationLimitVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpecificationLimitVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartProcessCharacteristicId = table.Column<int>(type: "int", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    SourceKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SourceSegmentId = table.Column<int>(type: "int", nullable: true),
                    ChangeType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PreviousSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecificationLimitVersions", x => x.Id);
                },
                comment: "工程規格不可變版本快照，僅記錄追蹤啟用後異動");

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationLimitVersions_PartProcessCharacteristicId_ChangedAt",
                table: "SpecificationLimitVersions",
                columns: new[] { "PartProcessCharacteristicId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SpecificationLimitVersions_PartProcessCharacteristicId_VersionNo",
                table: "SpecificationLimitVersions",
                columns: new[] { "PartProcessCharacteristicId", "VersionNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecificationLimitVersions");
        }
    }
}
