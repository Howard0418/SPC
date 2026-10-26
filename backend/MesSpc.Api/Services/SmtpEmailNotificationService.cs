using MesSpc.Api.Services.Localization;
using System.Net;
using System.Net.Mail;
using System.Text;
using MesSpc.Api.Domain.Entities;

namespace MesSpc.Api.Services;

public class SmtpEmailNotificationService(IConfiguration config, ILogger<SmtpEmailNotificationService> logger) : IEmailNotificationService
{
    public async Task<bool> SendAlertEmailAsync(AlertEvent alert, string recipientEmail, string recipientName, SmtpSettingsOverride? overrideSettings = null, string? uiLocale = null)
    {
        var content = SpcMailContent.Alert(alert, recipientName, uiLocale);
        return await SendEmailInternalAsync(recipientEmail, recipientName, content.Subject, content.HtmlBody, overrideSettings, uiLocale, content.LegacyFileSubject);
    }

    public async Task<bool> SendTestEmailAsync(string recipientEmail, SmtpSettingsOverride? overrideSettings = null, string? uiLocale = null)
    {
        var activeHost = overrideSettings?.Host ?? config["SmtpSettings:Host"] ?? "localhost";
        var activePort = overrideSettings != null ? overrideSettings.Port : int.TryParse(config["SmtpSettings:Port"], out var p) ? p : 25;
        var content = SpcMailContent.SmtpTest(activeHost, activePort, DateTime.UtcNow, uiLocale);
        return await SendEmailInternalAsync(recipientEmail, SpcMailContent.TestRecipient(uiLocale), content.Subject, content.HtmlBody, overrideSettings, uiLocale, content.LegacyFileSubject);
    }

    public Task<bool> SendHtmlEmailAsync(string recipientEmail, string recipientName, string subject, string htmlBody, SmtpSettingsOverride? overrideSettings = null, string? uiLocale = null, string? fileSubject = null)
        => SendEmailInternalAsync(recipientEmail, recipientName, subject, htmlBody, overrideSettings, uiLocale, fileSubject);

    public async Task<bool> SendReportEmailAsync(string recipientEmail, string recipientName, string subject, string htmlBody, byte[] excelBytes, string fileName, string? uiLocale = null)
    {
        var host = config["SmtpSettings:Host"] ?? "localhost";
        var port = int.TryParse(config["SmtpSettings:Port"], out var p) ? p : 25;
        var sender = config["SmtpSettings:SenderEmail"] ?? "mes-spc-report@pmr-spc.internal";
        using var mail = new MailMessage
        {
            From = new MailAddress(sender, SpcReportPresentation.Text("reportMailSender", uiLocale)),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8
        };
        mail.To.Add(new MailAddress(recipientEmail, recipientName));
        mail.Attachments.Add(new Attachment(new MemoryStream(excelBytes), fileName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = bool.TryParse(config["SmtpSettings:EnableSsl"], out var ssl) && ssl
        };
        var user = config["SmtpSettings:Username"];
        var pass = config["SmtpSettings:Password"];
        if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
            smtp.Credentials = new NetworkCredential(user, pass);
        try
        {
            await smtp.SendMailAsync(mail);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SPC 排程報表寄送失敗：{Email}", recipientEmail);
            return false;
        }
    }

    private async Task<bool> SendEmailInternalAsync(string toEmail, string toName, string subject, string htmlBody, SmtpSettingsOverride? overrideSettings = null, string? uiLocale = null, string? fileSubject = null)
    {
        var host = overrideSettings?.Host ?? config["SmtpSettings:Host"] ?? "localhost";
        var port = overrideSettings != null ? overrideSettings.Port : int.TryParse(config["SmtpSettings:Port"], out var p) ? p : 25;
        var user = overrideSettings?.Username ?? config["SmtpSettings:Username"] ?? "";
        var pass = overrideSettings?.Password ?? config["SmtpSettings:Password"] ?? "";
        var sender = overrideSettings?.SenderEmail ?? config["SmtpSettings:SenderEmail"] ?? "mes-spc-alert@pmr-spc.internal";
        var enableSsl = overrideSettings != null ? overrideSettings.EnableSsl : bool.TryParse(config["SmtpSettings:EnableSsl"], out var s) && s;
        var saveToLocal = overrideSettings != null ? overrideSettings.SaveToLocalDisk : !bool.TryParse(config["SmtpSettings:SaveToLocalDisk"], out var l) || l;
        var localFolder = overrideSettings?.LocalDiskFolder ?? config["SmtpSettings:LocalDiskFolder"] ?? @"C:\Users\ihao_ting.PMR.000\Desktop\MES\EmailOutbox";

        // 1. Save HTML copy to Local Outbox directory for immediate viewing
        if (saveToLocal)
        {
            try
            {
                if (!Directory.Exists(localFolder)) Directory.CreateDirectory(localFolder);
                var safeSubject = string.Join("_", (fileSubject ?? subject).Split(Path.GetInvalidFileNameChars()));
                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeSubject}.html";
                var filePath = Path.Combine(localFolder, fileName);
                
                var fullDoc = $"<!DOCTYPE html><html><head><meta charset='utf-8'><title>{WebUtility.HtmlEncode(subject)}</title></head><body style='background:#f1f5f9;padding:20px;'>{htmlBody}</body></html>";
                await File.WriteAllTextAsync(filePath, fullDoc, Encoding.UTF8);
                logger.LogInformation("已於本地目錄儲存通報郵件副本：{FilePath}", filePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "儲存通報郵件副本至本地目錄失敗");
            }
        }

        // 2. Send via SMTP Client
        try
        {
            using var mail = new MailMessage();
            mail.From = new MailAddress(sender, SpcMailContent.SenderName(uiLocale));
            mail.To.Add(new MailAddress(toEmail, toName));
            mail.Subject = subject;
            mail.Body = htmlBody;
            mail.IsBodyHtml = true;
            mail.BodyEncoding = Encoding.UTF8;

            using var smtp = new SmtpClient(host, port);
            smtp.EnableSsl = enableSsl;
            if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
            {
                smtp.Credentials = new NetworkCredential(user, pass);
            }
            
            await smtp.SendMailAsync(mail);
            logger.LogInformation("SMTP 郵件已成功發送至 {Email} (Host: {Host}:{Port})", toEmail, host, port);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SMTP 伺服器傳送郵件失敗 ({Host}:{Port}) (伺服器可能未執行或遭防火牆阻擋)；但郵件副本已生成於本地目錄。", host, port);
            return false; // Still considered successful locally if saved to disk
        }
    }

}
