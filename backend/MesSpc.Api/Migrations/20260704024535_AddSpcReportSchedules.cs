using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSpcReportSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpcReportSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecipientOperatorIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WeeklyEnabled = table.Column<bool>(type: "bit", nullable: false),
                    WeeklyDayOfWeek = table.Column<int>(type: "int", nullable: false),
                    MonthlyEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MonthlyDay = table.Column<int>(type: "int", nullable: false),
                    SendTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    LastWeeklySentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastMonthlySentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpcReportSchedules", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpcReportSchedules");
        }
    }
}
