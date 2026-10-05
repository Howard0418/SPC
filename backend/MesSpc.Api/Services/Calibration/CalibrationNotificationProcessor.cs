using System.Net;
using System.Net.Mail;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesSpc.Api.Services.Calibration;

public record CalibrationMailResult(string State,string? ErrorCode);
public interface ICalibrationMailSender
{ Task<CalibrationMailResult> SendAsync(string email,string subject,string body,CancellationToken ct); }

/// <summary>共用 SMTP 設定但獨立校正結果；沒有明確啟用不連線 SMTP。</summary>
public sealed class CalibrationSmtpSender(IConfiguration config,ILogger<CalibrationSmtpSender> logger) : ICalibrationMailSender
{
    public async Task<CalibrationMailResult> SendAsync(string email,string subject,string body,CancellationToken ct)
    {
        if (!config.GetValue<bool>("Calibration:DeliveryEnabled")) return new("Failed","DeliveryDisabled");
        var host=config["SmtpSettings:Host"]; var sender=config["SmtpSettings:SenderEmail"];
        if(string.IsNullOrWhiteSpace(host) || !InstrumentCalibrationService.ValidEmail(sender)) return new("Failed","SmtpNotConfigured");
        using var mail=new MailMessage(sender!,email,subject,body){IsBodyHtml=true};
        using var smtp=new SmtpClient(host,config.GetValue("SmtpSettings:Port",25)){EnableSsl=config.GetValue<bool>("SmtpSettings:EnableSsl")};
        if(!string.IsNullOrWhiteSpace(config["SmtpSettings:Username"])) smtp.Credentials=new NetworkCredential(config["SmtpSettings:Username"],config["SmtpSettings:Password"]);
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(45));
        try { await smtp.SendMailAsync(mail,timeout.Token); return new("Sent",null); }
        catch(SmtpFailedRecipientException ex) { logger.LogWarning(ex,"校正通知收件人被拒絕"); return new("Failed","RecipientRejected"); }
        catch(SmtpException ex) when ((int)ex.StatusCode>=400) { logger.LogWarning(ex,"校正通知被 SMTP 拒絕"); return new("Failed","SmtpRejected"); }
        catch(Exception ex) when(ex is SmtpException or OperationCanceledException or IOException)
        { logger.LogWarning(ex,"校正通知投遞結果不確定，需人工核對"); return new("Unknown","TransportUncertain"); }
    }
}

/// <summary>持久化補查與逐收件人寄送；租約到期的 Sending 轉 Unknown 而非重寄。</summary>
public sealed class CalibrationNotificationProcessor(AppDbContext db,TimeProvider clock,ICalibrationMailSender mail,ILogger<CalibrationNotificationProcessor> logger,
    ICalibrationChatSender? chat = null, CalibrationChatSecrets? secrets = null, IConfiguration? config = null)
{
    public async Task ProcessAsync(CancellationToken ct)
    {
        var now=clock.GetUtcNow().UtcDateTime;
        var expired=await db.Set<CalibrationNotification>().Where(x=>x.State=="Sending" && x.LeaseUntil<now).ToListAsync(ct);
        foreach(var n in expired) { n.State="Unknown"; n.ErrorCode="SendingLeaseExpired"; n.Version=Guid.NewGuid();
            db.Add(new CalibrationNotificationAttempt{NotificationId=n.Id,AttemptedAt=now,Result="Unknown",ErrorCode=n.ErrorCode}); }
        await db.SaveChangesAsync(ct);
        var settings=await db.Set<CalibrationNotificationSetting>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==1,ct);
        if(settings?.IsEnabled!=true) return;
        var days=CalibrationRules.ValidateReminderDays(JsonSerializer.Deserialize<int[]>(settings.ReminderDaysJson) ?? []);
        var today=CalibrationRules.Today(clock.GetUtcNow());
        // Polling is intentional: persistent stage keys make the daily check safe after restarts,
        // while a new instrument or changed recipients can be caught later the same day.
        if(CalibrationRules.ShouldScan(clock.GetUtcNow()))
        {
            var instruments=await db.Set<CalibrationInstrument>().Where(x=>x.UsageStatus=="Active").ToListAsync(ct);
            foreach(var i in instruments) await EnqueueAsync(i,today,days,now,ct);
        }
        var ids=await db.Set<CalibrationNotification>().AsNoTracking()
            .Where(x=>x.State=="Pending" || (x.State=="Failed" && x.NextAttemptAt!=null && x.NextAttemptAt<=now))
            .OrderBy(x=>x.Id).Select(x=>x.Id).Take(100).ToListAsync(ct);
        foreach(var id in ids) await DeliverAsync(id,today,days,ct);
    }

    private async Task<string[]> RecipientsAsync(CalibrationInstrument i,CancellationToken ct)
    {
        var setting = await db.Set<CalibrationNotificationSetting>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (setting?.NotificationChannel == "SynologyChat")
        {
            if (secrets is null || string.IsNullOrWhiteSpace(setting.ChatWebhookProtected)) return [];
            try { return [CalibrationChatSecrets.RecipientKey(secrets.Unprotect(setting.ChatWebhookProtected))]; }
            catch (System.Security.Cryptography.CryptographicException) { logger.LogWarning("Chat Webhook 無法解密，請重新設定"); return []; }
        }
        if (setting is not null && setting.NotificationChannel != "Email") return [];
        var ids=(JsonSerializer.Deserialize<int[]>(i.RecipientOperatorIdsJson) ?? []).ToHashSet();
        if(i.IncludeCustodian) ids.Add(i.CustodianOperatorId);
        var emails=await db.Operators.AsNoTracking().Where(x=>ids.Contains(x.Id) && x.IsActive).Select(x=>x.Email).ToListAsync(ct);
        return emails.Where(InstrumentCalibrationService.ValidEmail).Select(x=>x!.Trim().ToLowerInvariant()).Distinct().ToArray();
    }
    private async Task EnqueueAsync(CalibrationInstrument i,DateOnly today,int[] days,DateTime now,CancellationToken ct)
    {
        if (!i.NextCalibrationDate.HasValue) return;
        var due=i.NextCalibrationDate.Value;
        var stage=CalibrationRules.Stage(due,today,days); if(stage is null) return;
        var recipients=await RecipientsAsync(i,ct);
        var setting=await db.Set<CalibrationNotificationSetting>().AsNoTracking().SingleAsync(x=>x.Id==1,ct);
        i.NotificationIssue=recipients.Length==0?(setting.NotificationChannel=="SynologyChat"?"ChatNotConfigured":"NoValidRecipients"):null;
        var stages=days.Where(d=>today>=due.AddDays(-d)).Select(d=>$"BEFORE-{d}").ToList();
        // Summarize skipped overdue intervals instead of creating unbounded historical rows.
        if(stage.StartsWith("OVERDUE-")) stages.Add(stage);
        foreach(var email in recipients)
            foreach(var s in stages.Distinct())
                if(!await db.Set<CalibrationNotification>().AnyAsync(x=>x.InstrumentId==i.Id && x.CycleId==i.CurrentCycleId && x.DueDate==due && x.Stage==s && x.RecipientKey==email,ct))
                    db.Add(new CalibrationNotification{InstrumentId=i.Id,CycleId=i.CurrentCycleId,DueDate=due,Stage=s,RecipientKey=email,RecipientEmail=setting.NotificationChannel=="Email"?email:"",Channel=setting.NotificationChannel,
                        State=s==stage?"Pending":"Skipped",CreatedAt=now,ErrorCode=s==stage?null:"SupersededByCurrentStage"});
        try { await db.SaveChangesAsync(ct); }
        catch(DbUpdateException ex) { logger.LogWarning(ex,"校正掃描併發衝突，保留既有工作並於下輪重查"); db.ChangeTracker.Clear(); }
    }

    private async Task DeliverAsync(int id,DateOnly today,int[] days,CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        var now=clock.GetUtcNow().UtcDateTime;
        var n=await db.Set<CalibrationNotification>().SingleAsync(x=>x.Id==id,ct);
        if(n.State!="Pending" && !(n.State=="Failed" && n.NextAttemptAt<=now)) return;
        await using(var tx=await db.Database.BeginTransactionAsync(ct))
        {
            var i=await db.Set<CalibrationInstrument>().SingleAsync(x=>x.Id==n.InstrumentId,ct);
            var currentSetting=await db.Set<CalibrationNotificationSetting>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==1,ct);
            var enabled=currentSetting?.IsEnabled==true && currentSetting.NotificationChannel==n.Channel;
            if(!enabled || !CalibrationRules.CanNotify(i.UsageStatus) || i.CurrentCycleId!=n.CycleId || i.NextCalibrationDate!=n.DueDate || !(await RecipientsAsync(i,ct)).Contains(n.RecipientKey))
            { n.State="Cancelled"; n.ErrorCode="NoLongerEligible"; }
            else if(CalibrationRules.Stage(n.DueDate,today,days)!=n.Stage)
            { n.State="Skipped"; n.ErrorCode="SupersededByCurrentStage"; }
            else
            { n.State="Sending"; n.LeaseUntil=now.AddMinutes(5); n.AttemptCount++; i.Version=Guid.NewGuid(); }
            n.NextAttemptAt=null; n.Version=Guid.NewGuid();
            try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
            catch(DbUpdateConcurrencyException) { return; }
        }
        if(n.State!="Sending") return;
        var instrument=await db.Set<CalibrationInstrument>().AsNoTracking().SingleAsync(x=>x.Id==n.InstrumentId,ct);
        CalibrationMailResult result;
        try
        {
            var subject = $"【儀器校正提醒】{instrument.Code} {instrument.Name}";
            if (n.Channel == "SynologyChat")
            {
                var current = await db.Set<CalibrationNotificationSetting>().AsNoTracking().SingleAsync(x => x.Id == 1, ct);
                if (config?.GetValue<bool>("Calibration:DeliveryEnabled") != true)
                    result = new("Failed", "DeliveryDisabled");
                else if (chat is null || secrets is null || string.IsNullOrWhiteSpace(current.ChatWebhookProtected))
                    result = new("Failed", "ChatNotConfigured");
                else
                {
                    var url = secrets.Unprotect(current.ChatWebhookProtected);
                    if (!current.IsEnabled || current.NotificationChannel != n.Channel || CalibrationChatSecrets.RecipientKey(url) != n.RecipientKey)
                        result = new("Cancelled", "NoLongerEligible");
                    else
                        result = await chat.SendAsync(url, $"{subject}\n儀器：{instrument.Code}／{instrument.Name}\n校正期限：{n.DueDate:yyyy-MM-dd}\n提醒階段：{n.Stage}\n請登入 SPC 儀器校正管理查閱。", ct);
                }
            }
            else result=await mail.SendAsync(n.RecipientEmail,subject,
                $"<p>儀器：{WebUtility.HtmlEncode(instrument.Code)}／{WebUtility.HtmlEncode(instrument.Name)}</p><p>校正期限：{n.DueDate:yyyy-MM-dd}</p><p>提醒階段：{WebUtility.HtmlEncode(n.Stage)}</p><p>請登入 SPC 儀器校正管理查閱。</p>",ct);
        }
        catch(Exception ex) { logger.LogError("校正寄送結果不確定，工作 {Id}，類型 {ErrorType}",n.Id,ex.GetType().Name); result=new("Unknown","UnhandledTransportError"); }
        n.State=result.State; n.ErrorCode=result.ErrorCode; n.LeaseUntil=null; n.Version=Guid.NewGuid();
        if(result.State=="Sent") n.SentAt=clock.GetUtcNow().UtcDateTime;
        if(result.State=="Failed" && CalibrationRules.RetryDelay(n.AttemptCount) is {} delay) n.NextAttemptAt=clock.GetUtcNow().UtcDateTime.Add(delay);
        db.Add(new CalibrationNotificationAttempt{NotificationId=n.Id,AttemptedAt=clock.GetUtcNow().UtcDateTime,Result=n.State,ErrorCode=n.ErrorCode});
        // A database failure here leaves persisted Sending; expiration marks Unknown, never re-sends.
        await db.SaveChangesAsync(ct);
    }
}

public sealed class CalibrationNotificationSchedulerService(IServiceScopeFactory scopes,ILogger<CalibrationNotificationSchedulerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try { using var scope=scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<CalibrationNotificationProcessor>().ProcessAsync(stoppingToken); }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { break; }
            catch(Exception ex) { logger.LogError(ex,"校正通知背景工作失敗"); }
            try { await Task.Delay(TimeSpan.FromMinutes(1),stoppingToken); }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
