using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCalibrationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 儀器主檔
            migrationBuilder.CreateTable(
                name: "CalibrationInstruments",
                comment: "儀器校正主檔",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false, comment: "校正模組 Code"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "校正模組 Name"),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "校正模組 Department"),
                    CustodianOperatorId = table.Column<int>(type: "int", nullable: false, comment: "校正模組 CustodianOperatorId"),
                    CycleMonths = table.Column<int>(type: "int", nullable: false, defaultValue: 12, comment: "校正模組 CycleMonths"),
                    LastCalibrationDate = table.Column<DateOnly>(type: "date", nullable: true, comment: "校正模組 LastCalibrationDate"),
                    NextCalibrationDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "校正模組 NextCalibrationDate"),
                    UsageStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Active", comment: "校正模組 UsageStatus"),
                    LatestResult = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "校正模組 LatestResult"),
                    CurrentCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "校正模組 CurrentCycleId"),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "校正模組 Version"),
                    RecipientOperatorIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]", comment: "校正模組 RecipientOperatorIdsJson"),
                    IncludeCustodian = table.Column<bool>(type: "bit", nullable: false, comment: "校正模組 IncludeCustodian"),
                    NotificationIssue = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "校正模組 NotificationIssue")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationInstruments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalibrationInstruments_Operators_CustodianOperatorId",
                        column: x => x.CustodianOperatorId,
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationInstruments_Code",
                table: "CalibrationInstruments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationInstruments_CustodianOperatorId",
                table: "CalibrationInstruments",
                column: "CustodianOperatorId");

            // 校正歷史紀錄
            migrationBuilder.CreateTable(
                name: "InstrumentCalibrationRecords",
                comment: "不可覆寫的校正歷史",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentId = table.Column<int>(type: "int", nullable: false, comment: "校正模組 InstrumentId"),
                    CalibrationDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "校正模組 CalibrationDate"),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Passed", comment: "校正模組 Result"),
                    PreviousDueDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "校正模組 PreviousDueDate"),
                    NextDueDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "校正模組 NextDueDate"),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 Reason"),
                    CorrectsRecordId = table.Column<int>(type: "int", nullable: true, comment: "校正模組 CorrectsRecordId"),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "校正模組 RequestId"),
                    RequestHash = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 RequestHash"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 CreatedBy"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "校正模組 CreatedAt")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentCalibrationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstrumentCalibrationRecords_CalibrationInstruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "CalibrationInstruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstrumentCalibrationRecords_InstrumentCalibrationRecords_CorrectsRecordId",
                        column: x => x.CorrectsRecordId,
                        principalTable: "InstrumentCalibrationRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentCalibrationRecords_InstrumentId_RequestId",
                table: "InstrumentCalibrationRecords",
                columns: new[] { "InstrumentId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentCalibrationRecords_CorrectsRecordId",
                table: "InstrumentCalibrationRecords",
                column: "CorrectsRecordId");

            // 校正證書附件索引
            migrationBuilder.CreateTable(
                name: "CalibrationCertificates",
                comment: "校正證書私有檔案索引",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalibrationRecordId = table.Column<int>(type: "int", nullable: false, comment: "校正模組 CalibrationRecordId"),
                    StorageKey = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 StorageKey"),
                    OriginalName = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 OriginalName"),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 ContentType"),
                    Size = table.Column<long>(type: "bigint", nullable: false, comment: "校正模組 Size"),
                    Hash = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 Hash"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 CreatedBy"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "校正模組 CreatedAt")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalibrationCertificates_InstrumentCalibrationRecords_CalibrationRecordId",
                        column: x => x.CalibrationRecordId,
                        principalTable: "InstrumentCalibrationRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationCertificates_CalibrationRecordId",
                table: "CalibrationCertificates",
                column: "CalibrationRecordId");

            // 通知設定（單列 Id=1）
            migrationBuilder.CreateTable(
                name: "CalibrationNotificationSettings",
                comment: "校正提醒設定",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ReminderDaysJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[30,7,0]", comment: "校正模組 ReminderDaysJson"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, comment: "校正模組 IsEnabled"),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "校正模組 Version")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationNotificationSettings", x => x.Id);
                });

            // 逐收件人通知工作
            migrationBuilder.CreateTable(
                name: "CalibrationNotifications",
                comment: "逐收件人校正通知工作",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentId = table.Column<int>(type: "int", nullable: false, comment: "校正模組 InstrumentId"),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "校正模組 CycleId"),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false, comment: "校正模組 DueDate"),
                    Stage = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "", comment: "校正模組 Stage"),
                    RecipientKey = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false, defaultValue: "", comment: "校正模組 RecipientKey"),
                    RecipientEmail = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 RecipientEmail"),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending", comment: "校正模組 State"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "校正模組 CreatedAt"),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "校正模組 NextAttemptAt"),
                    LeaseUntil = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "校正模組 LeaseUntil"),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, comment: "校正模組 AttemptCount"),
                    ErrorCode = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "校正模組 ErrorCode"),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "校正模組 SentAt"),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "校正模組 Version")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalibrationNotifications_CalibrationInstruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "CalibrationInstruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationNotifications_InstrumentId_CycleId_DueDate_Stage_RecipientKey",
                table: "CalibrationNotifications",
                columns: new[] { "InstrumentId", "CycleId", "DueDate", "Stage", "RecipientKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationNotifications_State_NextAttemptAt",
                table: "CalibrationNotifications",
                columns: new[] { "State", "NextAttemptAt" });

            // 通知逐次嘗試紀錄
            migrationBuilder.CreateTable(
                name: "CalibrationNotificationAttempts",
                comment: "通知逐次嘗試與結果",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationId = table.Column<int>(type: "int", nullable: false, comment: "校正模組 NotificationId"),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "校正模組 AttemptedAt"),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 Result"),
                    ErrorCode = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "校正模組 ErrorCode")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationNotificationAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalibrationNotificationAttempts_CalibrationNotifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "CalibrationNotifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationNotificationAttempts_NotificationId",
                table: "CalibrationNotificationAttempts",
                column: "NotificationId");

            // 校正異動稽核日誌
            migrationBuilder.CreateTable(
                name: "CalibrationAuditLogs",
                comment: "校正異動稽核",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentId = table.Column<int>(type: "int", nullable: true, comment: "校正模組 InstrumentId"),
                    Actor = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 Actor"),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 Action"),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 BeforeJson"),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 AfterJson"),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "", comment: "校正模組 Reason"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "校正模組 CreatedAt")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationAuditLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CalibrationAuditLogs");
            migrationBuilder.DropTable(name: "CalibrationNotificationAttempts");
            migrationBuilder.DropTable(name: "CalibrationNotifications");
            migrationBuilder.DropTable(name: "CalibrationNotificationSettings");
            migrationBuilder.DropTable(name: "CalibrationCertificates");
            migrationBuilder.DropTable(name: "InstrumentCalibrationRecords");
            migrationBuilder.DropTable(name: "CalibrationInstruments");
        }
    }
}
