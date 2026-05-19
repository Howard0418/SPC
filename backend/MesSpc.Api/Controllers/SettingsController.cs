using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Route("api/v1/settings")]
public class SettingsController(IConfiguration config, IWebHostEnvironment env, IEmailNotificationService emailService, ILogger<SettingsController> logger) : ControllerBase
{
    [HttpGet("smtp")]
    public IActionResult GetSmtpSettings()
    {
        var section = config.GetSection("SmtpSettings");
        var dto = new SmtpSettingsDto
        {
            Host = section["Host"] ?? "localhost",
            Port = int.TryParse(section["Port"], out var p) ? p : 25,
            Username = section["Username"] ?? "",
            Password = section["Password"] ?? "",
            SenderEmail = section["SenderEmail"] ?? "spc-alert@pmr.com.tw",
            DefaultRecipientEmail = section["DefaultRecipientEmail"] ?? "ihao_ting@pmr.com.tw",
            EnableSsl = bool.TryParse(section["EnableSsl"], out var ssl) && ssl,
            SaveToLocalDisk = !bool.TryParse(section["SaveToLocalDisk"], out var save) || save,
            LocalDiskFolder = section["LocalDiskFolder"] ?? @"C:\Users\ihao_ting.PMR.000\Desktop\MES\EmailOutbox"
        };
        return Ok(dto);
    }

    [HttpPost("smtp")]
    public async Task<IActionResult> UpdateSmtpSettings([FromBody] SmtpSettingsDto dto)
    {
        var filePath = Path.Combine(env.ContentRootPath, "appsettings.json");
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { success = false, message = "找不到 appsettings.json 檔案。" });
        }

        try
        {
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            var node = JsonNode.Parse(json) as JsonObject ?? new JsonObject();

            var smtpObject = new JsonObject
            {
                ["Host"] = dto.Host,
                ["Port"] = dto.Port,
                ["Username"] = dto.Username ?? "",
                ["Password"] = dto.Password ?? "",
                ["SenderEmail"] = dto.SenderEmail,
                ["DefaultRecipientEmail"] = dto.DefaultRecipientEmail,
                ["EnableSsl"] = dto.EnableSsl,
                ["SaveToLocalDisk"] = dto.SaveToLocalDisk,
                ["LocalDiskFolder"] = dto.LocalDiskFolder
            };

            node["SmtpSettings"] = smtpObject;

            var options = new JsonSerializerOptions { WriteIndented = true };
            await System.IO.File.WriteAllTextAsync(filePath, node.ToJsonString(options));

            logger.LogInformation("SMTP 設定已更新至 appsettings.json");
            return Ok(new { success = true, message = "SMTP 設定已成功更新並生效。" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "更新 appsettings.json 時發生錯誤");
            return StatusCode(500, new { success = false, message = $"更新設定失敗: {ex.Message}" });
        }
    }

    [HttpPost("smtp/test")]
    public async Task<IActionResult> TestSmtp([FromBody] TestSmtpReq req)
    {
        var targetEmail = string.IsNullOrWhiteSpace(req.RecipientEmail) 
            ? config["SmtpSettings:DefaultRecipientEmail"] ?? "ihao_ting@pmr.com.tw" 
            : req.RecipientEmail.Trim();

        var overrideSettings = req.Settings != null 
            ? new SmtpSettingsOverride(
                req.Settings.Host, 
                req.Settings.Port, 
                req.Settings.Username, 
                req.Settings.Password, 
                req.Settings.SenderEmail, 
                req.Settings.EnableSsl, 
                req.Settings.SaveToLocalDisk, 
                req.Settings.LocalDiskFolder)
            : null;

        var activeHost = overrideSettings?.Host ?? config["SmtpSettings:Host"] ?? "localhost";
        var activePort = overrideSettings != null ? overrideSettings.Port : int.TryParse(config["SmtpSettings:Port"], out var p) ? p : 25;

        var success = await emailService.SendTestEmailAsync(targetEmail, overrideSettings);
        var folder = req.Settings?.LocalDiskFolder ?? config["SmtpSettings:LocalDiskFolder"] ?? @"C:\Users\ihao_ting.PMR.000\Desktop\MES\EmailOutbox";
        return Ok(new { success, recipient = targetEmail, host = activeHost, port = activePort, outboxFolder = folder });
    }

    [HttpPost("smtp/test-alert")]
    public async Task<IActionResult> TestAlertSmtp([FromBody] TestSmtpReq req)
    {
        var targetEmail = string.IsNullOrWhiteSpace(req.RecipientEmail) 
            ? config["SmtpSettings:DefaultRecipientEmail"] ?? "ihao_ting@pmr.com.tw" 
            : req.RecipientEmail.Trim();

        var overrideSettings = req.Settings != null 
            ? new SmtpSettingsOverride(
                req.Settings.Host, 
                req.Settings.Port, 
                req.Settings.Username, 
                req.Settings.Password, 
                req.Settings.SenderEmail, 
                req.Settings.EnableSsl, 
                req.Settings.SaveToLocalDisk, 
                req.Settings.LocalDiskFolder)
            : null;

        var activeHost = overrideSettings?.Host ?? config["SmtpSettings:Host"] ?? "localhost";
        var activePort = overrideSettings != null ? overrideSettings.Port : int.TryParse(config["SmtpSettings:Port"], out var p) ? p : 25;

        var dummyAlert = new MesSpc.Api.Domain.Entities.AlertEvent
        {
            Id = 8888,
            AlertType = MesSpc.Api.Domain.Enums.AlertType.OutOfSpec,
            ProductId = 101,
            StationId = 201,
            ActualValue = 105.85,
            Message = "測量值 105.85 超出規格上限 USL (100.00)",
            OccurredAt = DateTime.UtcNow,
            Status = "Open"
        };

        var success = await emailService.SendAlertEmailAsync(dummyAlert, targetEmail, "品管主管", overrideSettings);
        var folder = req.Settings?.LocalDiskFolder ?? config["SmtpSettings:LocalDiskFolder"] ?? @"C:\Users\ihao_ting.PMR.000\Desktop\MES\EmailOutbox";
        return Ok(new { success, recipient = targetEmail, host = activeHost, port = activePort, outboxFolder = folder });
    }
}

public class SmtpSettingsDto
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string SenderEmail { get; set; } = "spc-alert@pmr.com.tw";
    public string DefaultRecipientEmail { get; set; } = "ihao_ting@pmr.com.tw";
    public bool EnableSsl { get; set; }
    public bool SaveToLocalDisk { get; set; } = true;
    public string LocalDiskFolder { get; set; } = @"C:\Users\ihao_ting.PMR.000\Desktop\MES\EmailOutbox";
}

public record TestSmtpReq(string? RecipientEmail, SmtpSettingsDto? Settings);
