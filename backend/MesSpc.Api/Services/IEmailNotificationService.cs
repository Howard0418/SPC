using MesSpc.Api.Domain.Entities;

namespace MesSpc.Api.Services;

public interface IEmailNotificationService
{
    Task<bool> SendAlertEmailAsync(AlertEvent alertEvent, string recipientEmail, string recipientName);
    Task<bool> SendTestEmailAsync(string recipientEmail, SmtpSettingsOverride? overrideSettings = null);
}

public record SmtpSettingsOverride(string Host, int Port, string? Username, string? Password, string SenderEmail, bool EnableSsl, bool SaveToLocalDisk, string LocalDiskFolder);
