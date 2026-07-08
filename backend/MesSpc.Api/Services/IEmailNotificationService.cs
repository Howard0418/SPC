using MesSpc.Api.Domain.Entities;

namespace MesSpc.Api.Services;

public interface IEmailNotificationService
{
    Task<bool> SendAlertEmailAsync(AlertEvent alertEvent, string recipientEmail, string recipientName, SmtpSettingsOverride? overrideSettings = null);
    Task<bool> SendTestEmailAsync(string recipientEmail, SmtpSettingsOverride? overrideSettings = null);
    Task<bool> SendReportEmailAsync(string recipientEmail, string recipientName, string subject, string htmlBody, byte[] excelBytes, string fileName);
}

public record SmtpSettingsOverride(string Host, int Port, string? Username, string? Password, string SenderEmail, bool EnableSsl, bool SaveToLocalDisk, string LocalDiskFolder);
