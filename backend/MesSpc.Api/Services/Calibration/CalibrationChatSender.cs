using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace MesSpc.Api.Services.Calibration;

/// <summary>保護 Chat token，資料庫與對外 DTO 分離。</summary>
public sealed class CalibrationChatSecrets(IDataProtectionProvider provider)
{
    private readonly IDataProtector protector = provider.CreateProtector("SPC.Calibration.ChatWebhook.v1");
    public string Protect(string url) => protector.Protect(ValidateUrl(url).AbsoluteUri);
    public string Unprotect(string value) => protector.Unprotect(value);
    public static string RecipientKey(string url) =>
        "chat:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ValidateUrl(url).AbsoluteUri)));
    public static Uri ValidateUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 4096 ||
            !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && uri.Scheme != "http") ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment) ||
            uri.AbsolutePath != "/webapi/entry.cgi")
            throw new ArgumentException("請貼上 Synology Chat 的完整傳入 Webhook 網址。");
        var query = QueryHelpers.ParseQuery(uri.Query);
        if (query["api"] != "SYNO.Chat.External" || query["method"] != "incoming" ||
            string.IsNullOrWhiteSpace(query["token"]) || string.IsNullOrWhiteSpace(query["version"]))
            throw new ArgumentException("網址須為 Synology Chat 傳入 Webhook，並包含 token。");
        return uri;
    }
}

public interface ICalibrationChatSender
{
    Task<CalibrationMailResult> SendAsync(string url, string text, CancellationToken ct);
}

/// <summary>只送出 text；不使用會記錄完整 URI 的 HTTP logging handler。</summary>
public sealed class CalibrationChatSender(HttpClient client, ILogger<CalibrationChatSender> logger) : ICalibrationChatSender
{
    public async Task<CalibrationMailResult> SendAsync(string url, string text, CancellationToken ct)
    {
        var uri = CalibrationChatSecrets.ValidateUrl(url);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        { ["payload"] = JsonSerializer.Serialize(new { text }) });
        try
        {
            using var response = await client.PostAsync(uri, content, timeout.Token);
            if (!response.IsSuccessStatusCode)
                return new((int)response.StatusCode >= 500 ? "Unknown" : "Failed", "ChatHttpRejected");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            if (json.RootElement.ValueKind == JsonValueKind.Object &&
                json.RootElement.TryGetProperty("success", out var success))
            {
                if (success.ValueKind == JsonValueKind.True) return new("Sent", null);
                if (success.ValueKind == JsonValueKind.False) return new("Failed", "ChatRejected");
            }
            return new("Unknown", "ChatUnexpectedResponse");
        }
        catch (JsonException) { return new("Unknown", "ChatUnexpectedResponse"); }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            // Exception messages can contain the secret-bearing URI.
            logger.LogWarning("Synology Chat 傳輸結果不確定（{ErrorType}）", ex.GetType().Name);
            return new("Unknown", "ChatTransportUncertain");
        }
    }
}
