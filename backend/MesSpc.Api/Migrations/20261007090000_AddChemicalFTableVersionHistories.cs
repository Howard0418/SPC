using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    public partial class AddChemicalFTableVersionHistories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChemicalFTableVersionHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    FTableVersionId = table.Column<int>(type: "int", nullable: true),
                    VersionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreviousVersionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CellsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviousCellsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RestoredFromHistoryId = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChemicalFTableVersionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChemicalFTableVersionHistories_ChemicalFTableVersionHistories_RestoredFromHistoryId",
                        column: x => x.RestoredFromHistoryId,
                        principalTable: "ChemicalFTableVersionHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChemicalFTableVersionHistories_ChemicalFTableVersions_FTableVersionId",
                        column: x => x.FTableVersionId,
                        principalTable: "ChemicalFTableVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFTableVersionHistories_ChangedAt",
                table: "ChemicalFTableVersionHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFTableVersionHistories_FTableVersionId",
                table: "ChemicalFTableVersionHistories",
                column: "FTableVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFTableVersionHistories_RestoredFromHistoryId",
                table: "ChemicalFTableVersionHistories",
                column: "RestoredFromHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ChemicalFTableVersionHistories_VersionNo",
                table: "ChemicalFTableVersionHistories",
                column: "VersionNo",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ChemicalFTableVersionHistories");
        }
    }
}
