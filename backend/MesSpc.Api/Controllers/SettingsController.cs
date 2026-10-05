using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Services;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace MesSpc.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Route("api/v1/settings")]
public class SettingsController(IConfiguration config, IWebHostEnvironment env, IEmailNotificationService emailService, ILogger<SettingsController> logger, AppDbContext db) : ControllerBase
{
    [HttpGet("smtp")]
    public async Task<IActionResult> GetSmtpSettings()
    {
        var section = config.GetSection("SmtpSettings");
        var dto = new SmtpSettingsDto
        {
            Host = section["Host"] ?? "localhost",
            Port = int.TryParse(section["Port"], out var p) ? p : 25,
            Username = section["Username"] ?? "",
            Password = null,
            HasPassword = !string.IsNullOrWhiteSpace(section["Password"]),
            SenderEmail = section["SenderEmail"] ?? "spc-alert@pmr.com.tw",
            DefaultRecipientEmail = section["DefaultRecipientEmail"] ?? "ihao_ting@pmr.com.tw",
            EnableSsl = bool.TryParse(section["EnableSsl"], out var ssl) && ssl,
            SaveToLocalDisk = !bool.TryParse(section["SaveToLocalDisk"], out var save) || save,
            LocalDiskFolder = section["LocalDiskFolder"] ?? @"C:\Users\ihao_ting.PMR.000\Desktop\MES\EmailOutbox"
        };
        var operators = await db.Operators.AsNoTracking()
            .Where(x => x.IsActive && x.Email != null && x.Email != "")
            .OrderBy(x => x.Department).ThenBy(x => x.OperatorName)
            .Select(x => new { x.Id, x.OperatorCode, x.OperatorName, x.Department, x.Email })
            .ToListAsync();
        var setting = await db.SpcAlertNotificationSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();
        return Ok(new
        {
            smtp = dto,
            recipientOperatorIds = ParseRecipientIds(setting?.RecipientOperatorIdsJson),
            operators,
            departments = operators.Select(x => x.Department).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x)
        });
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

            var existingPassword = node["SmtpSettings"]?["Password"]?.GetValue<string>();
            var password = string.IsNullOrWhiteSpace(dto.Password) ? existingPassword : dto.Password;
            var smtpObject = new JsonObject
            {
                ["Host"] = dto.Host,
                ["Port"] = dto.Port,
                ["Username"] = dto.Username ?? "",
                ["Password"] = password ?? "",
                ["SenderEmail"] = dto.SenderEmail,
                ["DefaultRecipientEmail"] = dto.DefaultRecipientEmail,
                ["EnableSsl"] = dto.EnableSsl,
                ["SaveToLocalDisk"] = dto.SaveToLocalDisk,
                ["LocalDiskFolder"] = dto.LocalDiskFolder
            };

            node["SmtpSettings"] = smtpObject;

            var ids = dto.RecipientOperatorIds.Distinct().ToList();
            var validCount = await db.Operators.CountAsync(x => ids.Contains(x.Id) && x.IsActive && x.Email != null && x.Email != "");
            if (validCount != ids.Count)
                return BadRequest(new { success = false, message = "收件人包含停用或未設定 Email 的使用者。" });
            var setting = await db.SpcAlertNotificationSettings.FirstOrDefaultAsync() ?? new SpcAlertNotificationSetting();
            if (setting.Id == 0) db.SpcAlertNotificationSettings.Add(setting);
            setting.RecipientOperatorIdsJson = JsonSerializer.Serialize(ids);
            setting.IsEnabled = true;

            var options = new JsonSerializerOptions { WriteIndented = true };
            await System.IO.File.WriteAllTextAsync(filePath, node.ToJsonString(options));
            await db.SaveChangesAsync();

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

        var recipients = await ResolveRecipientsAsync(req.RecipientOperatorIds, req.RecipientEmail);
        var results = new List<TestRecipientResult>();
        foreach (var recipient in recipients)
        {
            try
            {
                var sent = await emailService.SendTestEmailAsync(recipient.Email, overrideSettings);
                results.Add(new(recipient.Email, recipient.Name, sent, sent ? null : "寄信服務回傳失敗。"));
            }
            catch (Exception ex) { results.Add(new(recipient.Email, recipient.Name, false, ex.Message)); }
        }
        var folder = req.Settings?.LocalDiskFolder ?? config["SmtpSettings:LocalDiskFolder"] ?? @"C:\Users\ihao_ting.PMR.000\Desktop\MES\EmailOutbox";
        return Ok(new { success = results.All(x => x.Success), recipients = results, host = activeHost, port = activePort, outboxFolder = folder });
    }

    [HttpPost("smtp/test-alert")]
    public async Task<IActionResult> TestAlertSmtp([FromBody] TestSmtpReq req)
    {
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
            PartId = 101,
            ProcessId = 201,
            CharacteristicId = 301,
            ActualValue = 105.85,
            Message = "測量值 105.85 超出規格上限 USL (100.00)",
            OccurredAt = DateTime.UtcNow,
            Status = "Open"
        };

        var recipients = await ResolveRecipientsAsync(req.RecipientOperatorIds, req.RecipientEmail);
        var results = new List<TestRecipientResult>();
        foreach (var recipient in recipients)
        {
            try
            {
                var sent = await emailService.SendAlertEmailAsync(dummyAlert, recipient.Email, recipient.Name, overrideSettings);
                results.Add(new(recipient.Email, recipient.Name, sent, sent ? null : "寄信服務回傳失敗。"));
            }
            catch (Exception ex) { results.Add(new(recipient.Email, recipient.Name, false, ex.Message)); }
        }
        var folder = req.Settings?.LocalDiskFolder ?? config["SmtpSettings:LocalDiskFolder"] ?? @"C:\Users\ihao_ting.PMR.000\Desktop\MES\EmailOutbox";
        return Ok(new { success = results.All(x => x.Success), recipients = results, host = activeHost, port = activePort, outboxFolder = folder });
    }

    private async Task<List<(string Email, string Name)>> ResolveRecipientsAsync(List<int> operatorIds, string? explicitEmail)
    {
        var ids = operatorIds.Distinct().ToList();
        if (ids.Count > 0)
        {
            return await db.Operators.AsNoTracking()
                .Where(x => ids.Contains(x.Id) && x.IsActive && x.Email != null && x.Email != "")
                .Select(x => new ValueTuple<string, string>(x.Email!, x.OperatorName))
                .ToListAsync();
        }
        var email = string.IsNullOrWhiteSpace(explicitEmail) ? config["SmtpSettings:DefaultRecipientEmail"] ?? "ihao_ting@pmr.com.tw" : explicitEmail.Trim();
        return [(email, "品管收件人")];
    }

    private static List<int> ParseRecipientIds(string? json)
    {
        try { return JsonSerializer.Deserialize<List<int>>(json ?? "[]") ?? []; }
        catch { return []; }
    }

    [HttpGet("inspect-excel")]
    public IActionResult InspectExcel([FromQuery] string path = @"C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\SPC管制項目.xlsx")
    {
        if (!System.IO.File.Exists(path)) return NotFound($"File not found at {path}");
        using var stream = System.IO.File.OpenRead(path);
        using var wb = new ClosedXML.Excel.XLWorkbook(stream);
        var sheets = new List<object>();
        foreach (var ws in wb.Worksheets)
        {
            var rows = new List<object>();
            var rCount = 0;
            foreach (var r in ws.RowsUsed())
            {
                if (rCount++ > 100) break;
                var cells = r.CellsUsed().Select(c => c.GetString()).ToList();
                rows.Add(new { row = r.RowNumber(), cells });
            }
            sheets.Add(new { name = ws.Name, rows });
        }
        return Ok(sheets);
    }
}

public class SmtpSettingsDto
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool HasPassword { get; set; }
    public string SenderEmail { get; set; } = "spc-alert@pmr.com.tw";
    public string DefaultRecipientEmail { get; set; } = "ihao_ting@pmr.com.tw";
    public List<int> RecipientOperatorIds { get; set; } = [];
    public bool EnableSsl { get; set; }
    public bool SaveToLocalDisk { get; set; } = true;
    public string LocalDiskFolder { get; set; } = @"C:\Users\ihao_ting.PMR.000\Desktop\MES\EmailOutbox";
}

public class TestSmtpReq
{
    public string? RecipientEmail { get; set; }
    public SmtpSettingsDto? Settings { get; set; }
    public List<int> RecipientOperatorIds { get; set; } = [];
}

public record TestRecipientResult(string Email, string Name, bool Success, string? ErrorMessage);
