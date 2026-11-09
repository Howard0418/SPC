using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLimitChangeHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LimitChangeBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LimitChangeBatches", x => x.Id);
                    table.CheckConstraint("CK_LimitChangeBatches_ItemCount", "[ItemCount] BETWEEN 1 AND 500");
                },
                comment: "Immutable header for atomic specification/control limit batch edits.");

            migrationBuilder.CreateTable(
                name: "LimitChangeItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LimitChangeBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartProcessCharacteristicId = table.Column<int>(type: "int", nullable: false),
                    PreviousLsl = table.Column<double>(type: "float", nullable: true),
                    PreviousTarget = table.Column<double>(type: "float", nullable: true),
                    PreviousUsl = table.Column<double>(type: "float", nullable: true),
                    PreviousLcl = table.Column<double>(type: "float", nullable: true),
                    PreviousCl = table.Column<double>(type: "float", nullable: true),
                    PreviousUcl = table.Column<double>(type: "float", nullable: true),
                    NewLsl = table.Column<double>(type: "float", nullable: true),
                    NewTarget = table.Column<double>(type: "float", nullable: true),
                    NewUsl = table.Column<double>(type: "float", nullable: true),
                    NewLcl = table.Column<double>(type: "float", nullable: true),
                    NewCl = table.Column<double>(type: "float", nullable: true),
                    NewUcl = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LimitChangeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LimitChangeItems_LimitChangeBatches_LimitChangeBatchId",
                        column: x => x.LimitChangeBatchId,
                        principalTable: "LimitChangeBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Immutable per-PPC before/after values for a limit change batch.");

            migrationBuilder.CreateIndex(
                name: "IX_LimitChangeBatches_ChangedAt",
                table: "LimitChangeBatches",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LimitChangeItems_LimitChangeBatchId_PartProcessCharacteristicId",
                table: "LimitChangeItems",
                columns: new[] { "LimitChangeBatchId", "PartProcessCharacteristicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LimitChangeItems_PartProcessCharacteristicId",
                table: "LimitChangeItems",
                column: "PartProcessCharacteristicId");

            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_LimitChangeBatches_Immutable]
                ON [LimitChangeBatches]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, N'界限批次歷史不可修改或刪除。', 1;
                END
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_LimitChangeItems_Immutable]
                ON [LimitChangeItems]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51000, N'界限批次明細不可修改或刪除。', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_LimitChangeItems_Immutable];");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_LimitChangeBatches_Immutable];");

            migrationBuilder.DropTable(
                name: "LimitChangeItems");

            migrationBuilder.DropTable(
                name: "LimitChangeBatches");
        }
    }
}
