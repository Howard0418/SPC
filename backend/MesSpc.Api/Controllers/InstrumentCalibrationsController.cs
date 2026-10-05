using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.Services.Calibration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Controllers;

/// <summary>儀器主檔、校正紀錄、證書、到期摘要與提醒設定。資料唯一來源。</summary>
[ApiController]
[Route("api/v1/instruments")]
public class InstrumentCalibrationsController(
    AppDbContext db,
    InstrumentCalibrationService calibrationService,
    CalibrationCertificates certificates,
    TimeProvider clock,
    IEmailNotificationService emailService,
    IConfiguration config) : ControllerBase
{
    private string GetActor() => User.Identity?.Name ?? "system";

    private bool CheckManagePermission()
    {
        // 取得目前呼叫者的角色與頁面權限 JSON
        var role = User.FindFirst("role")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Editor";
        var perms = User.FindFirst("permissions")?.Value;
        // 若在開發階段或無宣告 Claim，嘗試自資料庫讀取
        if (string.IsNullOrWhiteSpace(perms) && User.Identity?.Name != null)
        {
            var op = db.Operators.AsNoTracking().FirstOrDefault(x => x.Username == User.Identity.Name || x.OperatorCode == User.Identity.Name);
            if (op != null)
            {
                role = op.Role;
                perms = op.PagePermissionsJson;
            }
        }
        return CalibrationRules.CanManage(role, perms);
    }

    [HttpGet]
    public async Task<IActionResult> GetInstruments(
        [FromQuery] string? department,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);

        var query = db.Set<CalibrationInstrument>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(department))
            query = query.Where(x => x.Department == department);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.UsageStatus == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(search.Trim()));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // 附帶保管人姓名
        var custodianIds = items.Select(x => x.CustodianOperatorId).Distinct().ToArray();
        var custodians = await db.Operators.AsNoTracking()
            .Where(x => custodianIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => new { x.OperatorName, x.OperatorCode, x.Department }, ct);

        var result = items.Select(x => new
        {
            x.Id,
            x.Code,
            x.Name,
            x.Department,
            x.Location,
            x.CalibrationMethod,
            x.MeasurementSpecification,
            x.Precision,
            x.Remarks,
            x.CalibrationStandard,
            x.AcceptanceCriteria,

            x.CustodianOperatorId,
            CustodianName = custodians.TryGetValue(x.CustodianOperatorId, out var c) ? c.OperatorName : "",
            CustodianCode = custodians.TryGetValue(x.CustodianOperatorId, out var c2) ? c2.OperatorCode : "",
            x.CycleMonths,
            LastCalibrationDate = x.LastCalibrationDate?.ToString("yyyy-MM-dd"),
            NextCalibrationDate = x.NextCalibrationDate?.ToString("yyyy-MM-dd"),
            x.UsageStatus,
            x.LatestResult,
            x.IncludeCustodian,
            RecipientOperatorIds = JsonSerializer.Deserialize<int[]>(x.RecipientOperatorIdsJson) ?? [],
            x.NotificationIssue,
            x.Version
        });

        return Ok(new { success = true, data = result, total, page, pageSize });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetInstrument(int id, CancellationToken ct = default)
    {
        var item = await db.Set<CalibrationInstrument>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item == null) return NotFound(new { success = false, message = "找不到該儀器。" });

        var custodian = await db.Operators.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.CustodianOperatorId, ct);

        return Ok(new
        {
            success = true,
            data = new
            {
                item.Id,
                item.Code,
                item.Name,
                item.Department,
                item.Location,
                item.CalibrationMethod,
            item.MeasurementSpecification,
            item.Precision,
            item.Remarks,
            item.CalibrationStandard,
            item.AcceptanceCriteria,

                item.CustodianOperatorId,
                CustodianName = custodian?.OperatorName ?? "",
                CustodianCode = custodian?.OperatorCode ?? "",
                item.CycleMonths,
                LastCalibrationDate = item.LastCalibrationDate?.ToString("yyyy-MM-dd"),
                NextCalibrationDate = item.NextCalibrationDate?.ToString("yyyy-MM-dd"),
                item.UsageStatus,
                item.LatestResult,
                item.IncludeCustodian,
                RecipientOperatorIds = JsonSerializer.Deserialize<int[]>(item.RecipientOperatorIdsJson) ?? [],
                item.NotificationIssue,
                item.Version
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateInstrument([FromBody] InstrumentInput req, CancellationToken ct = default)
    {
        if (!CheckManagePermission())
            return StatusCode(403, new { success = false, message = "無維護儀器權限，需具備校正管理權限。" });

        try
        {
            var created = await calibrationService.SaveInstrumentAsync(null, req, GetActor(), ct);
            return Ok(new { success = true, data = created });
        }
        catch (CalibrationException ex)
        {
            return StatusCode(ex.Status, new { success = false, code = ex.Code, message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateInstrument(int id, [FromBody] InstrumentInput req, CancellationToken ct = default)
    {
        if (!CheckManagePermission())
            return StatusCode(403, new { success = false, message = "無維護儀器權限，需具備校正管理權限。" });

        try
        {
            var updated = await calibrationService.SaveInstrumentAsync(id, req, GetActor(), ct);
            return Ok(new { success = true, data = updated });
        }
        catch (CalibrationException ex)
        {
            return StatusCode(ex.Status, new { success = false, code = ex.Code, message = ex.Message });
        }
    }

    [HttpGet("{id:int}/calibrations")]
    public async Task<IActionResult> GetCalibrationHistory(int id, CancellationToken ct = default)
    {
        var instrument = await db.Set<CalibrationInstrument>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (instrument == null) return NotFound(new { success = false, message = "找不到該儀器。" });

        var records = await db.Set<InstrumentCalibrationRecord>().AsNoTracking()
            .Where(x => x.InstrumentId == id)
            .OrderByDescending(x => x.CalibrationDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);

        var recordIds = records.Select(x => x.Id).ToArray();
        var certs = await db.Set<CalibrationCertificate>().AsNoTracking()
            .Where(x => recordIds.Contains(x.CalibrationRecordId))
            .ToListAsync(ct);

        var certsByRecord = certs.GroupBy(x => x.CalibrationRecordId).ToDictionary(g => g.Key, g => g.Select(c => new
        {
            c.Id,
            c.OriginalName,
            c.ContentType,
            c.Size,
            c.CreatedAt
        }).ToList());

        var result = records.Select(x => new
        {
            x.Id,
            x.InstrumentId,
            CalibrationDate = x.CalibrationDate.ToString("yyyy-MM-dd"),
            x.Result,
            PreviousDueDate = x.PreviousDueDate.ToString("yyyy-MM-dd"),
            NextDueDate = x.NextDueDate.ToString("yyyy-MM-dd"),
            x.Reason,
            x.CorrectsRecordId,
            x.CreatedBy,
            CreatedAt = x.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            Certificates = certsByRecord.TryGetValue(x.Id, out var cList) ? cList : []
        });

        return Ok(new { success = true, data = result });
    }

    [HttpPost("{id:int}/calibrations")]
    public async Task<IActionResult> RecordCalibration(int id, [FromBody] CalibrationInput req, CancellationToken ct = default)
    {
        if (!CheckManagePermission())
            return StatusCode(403, new { success = false, message = "無登錄校正權限，需具備校正管理權限。" });

        try
        {
            var record = await calibrationService.RecordAsync(id, req, GetActor(), ct);
            return Ok(new { success = true, data = record });
        }
        catch (CalibrationException ex)
        {
            return StatusCode(ex.Status, new { success = false, code = ex.Code, message = ex.Message });
        }
    }

    [HttpPost("/api/v1/calibrations/{recordId:int}/certificates")]
    public async Task<IActionResult> UploadCertificate(int recordId, IFormFile file, CancellationToken ct = default)
    {
        if (!CheckManagePermission())
            return StatusCode(403, new { success = false, message = "無上傳證書權限，需具備校正管理權限。" });

        try
        {
            var cert = await certificates.UploadAsync(recordId, file, GetActor(), ct);
            return Ok(new { success = true, data = cert });
        }
        catch (CalibrationException ex)
        {
            return StatusCode(ex.Status, new { success = false, code = ex.Code, message = ex.Message });
        }
    }

    [HttpGet("/api/v1/calibration-certificates/{id:int}")]
    public async Task<IActionResult> DownloadCertificate(int id, CancellationToken ct = default)
    {
        var cert = await db.Set<CalibrationCertificate>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (cert == null) return NotFound(new { success = false, message = "找不到該證書附件。" });

        var path = certificates.Resolve(cert.StorageKey);
        if (!System.IO.File.Exists(path))
            return NotFound(new { success = false, message = "實體檔案不存在。" });

        var stream = System.IO.File.OpenRead(path);
        return File(stream, cert.ContentType, cert.OriginalName);
    }

    /// <summary>即將到期／已逾期件數；窗口跟隨提醒設定最大天數；含送校中、不含停用報廢。</summary>
    [HttpGet("/api/v1/instrument-calibrations/summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct = default)
    {
        var today = CalibrationRules.Today(clock.GetUtcNow());
        var settings = await db.Set<CalibrationNotificationSetting>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        var reminderDays = JsonSerializer.Deserialize<int[]>(settings?.ReminderDaysJson ?? "[30,7,0]") ?? [30, 7, 0];
        var windowDays = CalibrationRules.UpcomingWindowDays(reminderDays);

        var listed = await db.Set<CalibrationInstrument>().AsNoTracking()
            .Where(x => x.UsageStatus == "Active" || x.UsageStatus == "InCalibration")
            .Select(x => new { x.UsageStatus, x.NextCalibrationDate })
            .ToListAsync(ct);
        var counts = CalibrationRules.CountSummary(
            listed.Where(x => x.NextCalibrationDate.HasValue).Select(x => (x.UsageStatus, x.NextCalibrationDate!.Value)),
            today, windowDays);

        return Ok(new
        {
            success = true,
            data = new
            {
                asOfDate = today.ToString("yyyy-MM-dd"),
                timeZone = "Asia/Taipei",
                windowDays,
                upcomingCount = counts.Upcoming,
                dueTodayCount = counts.DueToday,
                overdueCount = counts.Overdue,
                inCalibrationCount = counts.InCalibration,
                generatedAt = clock.GetUtcNow().UtcDateTime.ToString("o")
            }
        });
    }

    /// <summary>取得提醒設定，不回傳 Webhook。</summary>
    [Authorize]
    [HttpGet("/api/v1/instrument-calibrations/settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct = default)
    {
        var settings = await db.Set<CalibrationNotificationSetting>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (settings == null)
        {
            settings = new CalibrationNotificationSetting { Id = 1, ReminderDaysJson = "[30,7,0]", IsEnabled = false };
        }
        return Ok(new
        {
            success = true,
            data = new
            {
                settings.Id,
                reminderDays = JsonSerializer.Deserialize<int[]>(settings.ReminderDaysJson) ?? [30, 7, 0],
                settings.IsEnabled,
                settings.NotificationChannel,
                chatWebhookConfigured = !string.IsNullOrWhiteSpace(settings.ChatWebhookProtected),
                settings.Version
            }
        });
    }

    /// <summary>儲存提醒管道；空白 Webhook 保留原值。</summary>
    [Authorize]
    [HttpPut("/api/v1/instrument-calibrations/settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] CalibrationSettingInput req, [FromServices] CalibrationChatSecrets chatSecrets, CancellationToken ct = default)
    {
        if (!CheckManagePermission())
            return StatusCode(403, new { success = false, message = "無修改提醒設定權限。" });

        int[] validatedDays;
        try
        {
            validatedDays = CalibrationRules.ValidateReminderDays(req.ReminderDays);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }

        var setting = await db.Set<CalibrationNotificationSetting>().FirstOrDefaultAsync(x => x.Id == 1, ct);
        var channel = req.NotificationChannel ?? setting?.NotificationChannel ?? "Email";
        if (channel is not ("Email" or "SynologyChat"))
            return BadRequest(new { success = false, message = "請選擇 Email 或 Synology Chat。" });
        var protectedUrl = setting?.ChatWebhookProtected;
        try
        {
            if (!string.IsNullOrWhiteSpace(req.ChatWebhookUrl)) protectedUrl = chatSecrets.Protect(req.ChatWebhookUrl);
        }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        if (channel == "SynologyChat" && string.IsNullOrWhiteSpace(protectedUrl))
            return BadRequest(new { success = false, message = "請先設定 Synology Chat Webhook 網址。" });
        var before = JsonSerializer.Serialize(new { setting?.NotificationChannel, setting?.IsEnabled, setting?.ReminderDaysJson });
        if (setting is null) { setting = new CalibrationNotificationSetting(); db.Add(setting); }
        setting.ReminderDaysJson = JsonSerializer.Serialize(validatedDays);
        setting.IsEnabled = req.IsEnabled;
        setting.NotificationChannel = channel;
        setting.ChatWebhookProtected = protectedUrl;
        setting.Version = Guid.NewGuid();
        db.Add(new CalibrationAuditLog
        {
            Actor = GetActor(), Action = "NotificationSettingsUpdated", BeforeJson = before,
            AfterJson = JsonSerializer.Serialize(new { channel, req.IsEnabled, reminderDays = validatedDays, webhookChanged = !string.IsNullOrWhiteSpace(req.ChatWebhookUrl) }),
            CreatedAt = clock.GetUtcNow().UtcDateTime
        });
        await db.SaveChangesAsync(ct);
        return await GetSettings(ct);
    }

    /// <summary>手動送出【測試】Chat 訊息，可使用輸入或已儲存的 Webhook；不新增排程紀錄。</summary>
    [Authorize]
    [HttpPost("/api/v1/instrument-calibrations/test-chat")]
    public async Task<IActionResult> SendTestChat([FromBody] CalibrationTestChatInput req,
        [FromServices] CalibrationChatSecrets chatSecrets, [FromServices] ICalibrationChatSender chat, CancellationToken ct = default)
    {
        if (!CheckManagePermission())
            return StatusCode(403, new { success = false, message = "無發送測試通知權限。" });
        var url = req.ChatWebhookUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            var setting = await db.Set<CalibrationNotificationSetting>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct);
            if (string.IsNullOrWhiteSpace(setting?.ChatWebhookProtected))
                return BadRequest(new { success = false, message = "請先貼上或儲存 Synology Chat Webhook 網址。" });
            try { url = chatSecrets.Unprotect(setting.ChatWebhookProtected); }
            catch (System.Security.Cryptography.CryptographicException)
            { return BadRequest(new { success = false, message = "Webhook 無法解密，請重新貼上並儲存。" }); }
        }
        try { CalibrationChatSecrets.ValidateUrl(url); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        var sample = await db.Set<CalibrationInstrument>().AsNoTracking()
            .Where(x => (x.UsageStatus == "Active" || x.UsageStatus == "InCalibration") && x.NextCalibrationDate != null)
            .OrderBy(x => x.NextCalibrationDate).ThenBy(x => x.Code).FirstOrDefaultAsync(ct);
        var text = $"【測試】儀器校正提醒\n儀器：{sample?.Code ?? "TEST-CAL"}／{sample?.Name ?? "測試樣本"}\n校正期限：{sample?.NextCalibrationDate?.ToString("yyyy-MM-dd") ?? CalibrationRules.Today(clock.GetUtcNow()).ToString("yyyy-MM-dd")}\n此為手動測試，非正式到期通知。請登入 SPC 儀器校正管理查閱。";
        var result = await chat.SendAsync(url!, text, ct);
        return Ok(new
        {
            success = result.State == "Sent",
            data = new { channel = "SynologyChat", state = result.State, result.ErrorCode },
            message = result.State == "Sent" ? "測試通知已送至 Synology Chat 群組。" :
                result.State == "Unknown" ? "傳送結果不確定，請先查看 Chat 群組，確認後再決定是否重試。" :
                "Synology Chat 拒絕通知，請檢查 Webhook 網址及群組設定。"
        });
    }

    /// <summary>手動寄出一封【測試】校正通知樣式信；不寫入到期通知紀錄，也不開啟每日自動寄信。</summary>
    [Authorize]
    [HttpPost("/api/v1/instrument-calibrations/test-email")]
    public async Task<IActionResult> SendTestEmail([FromBody] CalibrationTestEmailInput? req, CancellationToken ct = default)
    {
        if (!CheckManagePermission())
            return StatusCode(403, new { success = false, message = "無發送測試通知權限。" });

        var email = string.IsNullOrWhiteSpace(req?.Email) ? config["SmtpSettings:DefaultRecipientEmail"] : req!.Email.Trim();
        if (!InstrumentCalibrationService.ValidEmail(email))
            return BadRequest(new { success = false, message = "請指定有效的測試收件人 Email。" });
        var recipient = email!;

        var sample = await db.Set<CalibrationInstrument>().AsNoTracking()
            .Where(x => (x.UsageStatus == "Active" || x.UsageStatus == "InCalibration") && x.NextCalibrationDate != null)
            .OrderBy(x => x.NextCalibrationDate)
            .ThenBy(x => x.Code)
            .FirstOrDefaultAsync(ct);
        var settings = await db.Set<CalibrationNotificationSetting>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        var days = JsonSerializer.Deserialize<int[]>(settings?.ReminderDaysJson ?? "[30,7,0]") ?? [30, 7, 0];
        var today = CalibrationRules.Today(clock.GetUtcNow());
        var code = sample?.Code ?? "TEST-CAL";
        var name = sample?.Name ?? "測試樣本（非正式儀器）";
        var due = sample?.NextCalibrationDate ?? today;
        var stage = sample is null ? "TEST" : CalibrationRules.Stage(due, today, days) ?? "TEST";
        var subject = $"【測試】儀器校正提醒 {code} {name}";
        var body =
            $"<p>此為手動測試信，非正式到期通知，也不會寫入每日提醒紀錄。</p>" +
            $"<p>儀器：{System.Net.WebUtility.HtmlEncode(code)}／{System.Net.WebUtility.HtmlEncode(name)}</p>" +
            $"<p>校正期限：{due:yyyy-MM-dd}</p>" +
            $"<p>提醒階段：{System.Net.WebUtility.HtmlEncode(stage)}</p>" +
            "<p>請登入 SPC 儀器校正管理查閱。</p>";

        var sent = await emailService.SendHtmlEmailAsync(recipient, "校正測試收件人", subject, body);
        return Ok(new
        {
            success = sent,
            data = new
            {
                recipient,
                instrumentCode = code,
                instrumentName = name,
                dueDate = due.ToString("yyyy-MM-dd"),
                stage,
                smtpSent = sent,
                outboxFolder = config["SmtpSettings:LocalDiskFolder"]
            },
            message = sent ? "測試通知已寄出。" : "SMTP 未成功，請改查本機郵件備份資料夾。"
        });
    }
}

public record CalibrationSettingInput(int[] ReminderDays, bool IsEnabled, string? NotificationChannel = null, string? ChatWebhookUrl = null);
public record CalibrationTestChatInput(string? ChatWebhookUrl = null);
public record CalibrationTestEmailInput(string? Email);
