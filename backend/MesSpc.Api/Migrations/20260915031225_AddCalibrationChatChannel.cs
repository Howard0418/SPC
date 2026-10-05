using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MesSpc.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCalibrationChatChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "ChatWebhookProtected",
                table: "CalibrationNotificationSettings",
                type: "nvarchar(max)",
                maxLength: 8192,
                nullable: true,
                comment: "校正模組 ChatWebhookProtected");

            migrationBuilder.AddColumn<string>(
                name: "NotificationChannel",
                table: "CalibrationNotificationSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Email",
                comment: "校正模組 NotificationChannel");

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "CalibrationNotifications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Email",
                comment: "校正模組 Channel");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "ChatWebhookProtected",
                table: "CalibrationNotificationSettings");

            migrationBuilder.DropColumn(
                name: "NotificationChannel",
                table: "CalibrationNotificationSettings");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "CalibrationNotifications");

        }
    }
}
