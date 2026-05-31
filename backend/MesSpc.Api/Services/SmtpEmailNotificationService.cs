using System.Net;
using System.Net.Mail;
using System.Text;
using MesSpc.Api.Domain.Entities;

namespace MesSpc.Api.Services;

public class SmtpEmailNotificationService(IConfiguration config, ILogger<SmtpEmailNotificationService> logger) : IEmailNotificationService
{
    public async Task<bool> SendAlertEmailAsync(AlertEvent alert, string recipientEmail, string recipientName, SmtpSettingsOverride? overrideSettings = null)
    {
        var subject = $"【品質異常警報 {alert.AlertType}】料號/站別 {alert.PartId}-{alert.ProcessId} 數值異常";
        var body = GetHtmlBody(alert, recipientName);
        return await SendEmailInternalAsync(recipientEmail, recipientName, subject, body, overrideSettings);
    }

    public async Task<bool> SendTestEmailAsync(string recipientEmail, SmtpSettingsOverride? overrideSettings = null)
    {
        var activeHost = overrideSettings?.Host ?? config["SmtpSettings:Host"] ?? "localhost";
        var activePort = overrideSettings != null ? overrideSettings.Port : int.TryParse(config["SmtpSettings:Port"], out var p) ? p : 25;
        var subject = $"【MES+SPC 測試通知】自動化預警與電子郵件通報測試 ({activeHost}:{activePort})";
        var body = $@"
            <div style='font-family: Arial, sans-serif; border: 1px solid #cbd5e1; padding: 25px; border-radius: 12px; max-width: 650px; background-color: #ffffff; color: #1e293b;'>
                <div style='border-bottom: 2px solid #3b82f6; padding-bottom: 15px; margin-bottom: 20px; display: flex; items-center: center;'>
                    <h2 style='color: #2563eb; margin: 0; font-size: 22px;'>📢 MES + SPC 系統通報功能測試成功</h2>
                </div>
                <p style='font-size: 16px; line-height: 1.6;'>品管工程師您好：</p>
                <p style='font-size: 15px; line-height: 1.6; color: #475569;'>
                    這是一封由 <strong>Antigravity 系統架構</strong> 自動生成的測試信件，證明您的 MES 後端 SMTP 發信模組與本地儲存機制均已正常運作。
                </p>
                <div style='background-color: #f8fafc; border-left: 4px solid #10b981; padding: 15px; margin: 20px 0; border-radius: 0 8px 8px 0;'>
                    <p style='margin: 0; font-weight: bold; color: #065f46;'>測試發送時間：{DateTime.UtcNow.AddHours(8):yyyy-MM-dd HH:mm:ss} (台北時間)</p>
                    <p style='margin: 5px 0 0 0; font-size: 13px; color: #64748b;'>伺服器主機：{activeHost}:{activePort}</p>
                </div>
                <p style='font-size: 14px; color: #64748b;'>未來的實機 OOS / OOC 警報也將依此規格即時派發至負責人信箱。</p>
            </div>";
        return await SendEmailInternalAsync(recipientEmail, "品管測試員", subject, body, overrideSettings);
    }

    private async Task<bool> SendEmailInternalAsync(string toEmail, string toName, string subject, string htmlBody, SmtpSettingsOverride? overrideSettings = null)
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
                var safeSubject = string.Join("_", subject.Split(Path.GetInvalidFileNameChars()));
                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeSubject}.html";
                var filePath = Path.Combine(localFolder, fileName);
                
                var fullDoc = $"<!DOCTYPE html><html><head><meta charset='utf-8'><title>{subject}</title></head><body style='background:#f1f5f9;padding:20px;'>{htmlBody}</body></html>";
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
            mail.From = new MailAddress(sender, "MES+SPC 智慧預警通報");
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

    private static string GetHtmlBody(AlertEvent alert, string recipientName)
    {
        var badgeColor = alert.AlertType == Domain.Enums.AlertType.OutOfSpec ? "#dc2626" : "#ea580c";
        var badgeText = alert.AlertType == Domain.Enums.AlertType.OutOfSpec ? "規格違規 (OOS)" : "管制界限失控 (OOC)";
        var timestamp = alert.OccurredAt.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss");

        return $@"
            <div style='font-family: Arial, sans-serif; border: 1px solid #e2e8f0; padding: 25px; border-radius: 12px; max-width: 650px; background-color: #ffffff; color: #1e293b; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);'>
                <div style='border-bottom: 2px solid {badgeColor}; padding-bottom: 15px; margin-bottom: 20px; display: flex; align-items: center; justify-content: space-between;'>
                    <h2 style='color: {badgeColor}; margin: 0; font-size: 22px;'>🚨 工業品質即時警報通報</h2>
                    <span style='background-color: {badgeColor}; color: white; padding: 4px 10px; border-radius: 20px; font-size: 12px; font-weight: bold;'>{badgeText}</span>
                </div>

                <p style='font-size: 16px; line-height: 1.6;'>品管工程師 {recipientName} 您好：</p>
                <p style='font-size: 15px; line-height: 1.6; color: #475569;'>
                    系統於台北時間 <strong>{timestamp}</strong> 執行西方電氣規則判定與規格比對時，發現下列量測點位發生異常。
                </p>

                <table style='width: 100%; border-collapse: collapse; margin: 20px 0; font-size: 14px;'>
                    <tr style='background-color: #f8fafc;'>
                        <td style='padding: 12px; border: 1px solid #e2e8f0; width: 30%; color: #64748b; font-weight: bold;'>事件代號</td>
                        <td style='padding: 12px; border: 1px solid #e2e8f0; font-family: monospace; font-weight: bold; color: #0f172a;'>#ALT-{alert.Id:D5}</td>
                    </tr>
                    <tr>
                        <td style='padding: 12px; border: 1px solid #e2e8f0; color: #64748b; font-weight: bold;'>警報類型</td>
                        <td style='padding: 12px; border: 1px solid #e2e8f0; font-weight: bold; color: {badgeColor};'>{alert.AlertType}</td>
                    </tr>
                    <tr style='background-color: #f8fafc;'>
                        <td style='padding: 12px; border: 1px solid #e2e8f0; color: #64748b; font-weight: bold;'>產品料號 / 站別</td>
                        <td style='padding: 12px; border: 1px solid #e2e8f0; font-weight: bold;'>Part ID: {alert.PartId} / Process ID: {alert.ProcessId}</td>
                    </tr>
                    <tr>
                        <td style='padding: 12px; border: 1px solid #e2e8f0; color: #64748b; font-weight: bold;'>實際量測值</td>
                        <td style='padding: 12px; border: 1px solid #e2e8f0; font-family: monospace; font-size: 16px; font-weight: bold; color: #dc2626;'>{alert.ActualValue:F4}</td>
                    </tr>
                    <tr style='background-color: #f8fafc;'>
                        <td style='padding: 12px; border: 1px solid #e2e8f0; color: #64748b; font-weight: bold;'>違規判定原因</td>
                        <td style='padding: 12px; border: 1px solid #e2e8f0; color: #b91c1c; font-weight: 600;'>{alert.Message}</td>
                    </tr>
                </table>

                <div style='background-color: #fef2f2; border-left: 4px solid #ef4444; padding: 15px; margin: 20px 0; border-radius: 0 8px 8px 0;'>
                    <p style='margin: 0; font-size: 13px; color: #991b1b; line-height: 1.5;'>
                        <strong>處置指示：</strong> 本警報單已自動建檔並指派狀態為 <code>Open</code>。請負責人盡快登入 SPC 系統之「異常單簽核處置 (V2 Workflow)」頁面填寫真因分析 (Root Cause) 與對策，完成 IATF 16949 審核閉環。
                    </p>
                </div>

                <div style='margin-top: 30px; padding-top: 15px; border-top: 1px solid #e2e8f0; font-size: 12px; color: #94a3b8; text-align: center;'>
                    <p style='margin: 0;'>PMR Enterprise SPC 智慧製造中心 • 自動化品質通報機器人</p>
                    <p style='margin: 4px 0 0 0;'>此為系統自動發送之信件，請勿直接回覆。</p>
                </div>
            </div>";
    }
}
