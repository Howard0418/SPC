using System.Net;
using System.Text.Json;
using MesSpc.Api.Services.Calibration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;

namespace MesSpc.Calibration.Tests;

public class CalibrationChatTests
{
    internal const string Url = "https://nas.example.invalid/webapi/entry.cgi?api=SYNO.Chat.External&method=incoming&version=2&token=secret-test-only";
    [Theory]
    [InlineData("https://example.invalid/")]
    [InlineData("file:///webapi/entry.cgi")]
    [InlineData("https://user:pass@nas/webapi/entry.cgi?api=SYNO.Chat.External&method=incoming&version=2&token=x")]
    public void RejectsNonWebhookUrls(string url) => Assert.Throws<ArgumentException>(() => CalibrationChatSecrets.ValidateUrl(url));

    [Fact]
    public void EncryptsAndRoundTripsWithoutStoringUrl()
    {
        var secrets = new CalibrationChatSecrets(new EphemeralDataProtectionProvider());
        var encrypted = secrets.Protect(Url);
        Assert.DoesNotContain("secret-test-only", encrypted);
        Assert.Equal(Url, secrets.Unprotect(encrypted));
        Assert.Equal(CalibrationChatSecrets.RecipientKey(Url), CalibrationChatSecrets.RecipientKey(secrets.Unprotect(encrypted)));
    }

    [Theory]
    [InlineData(200, "{\"success\":true}", "Sent")]
    [InlineData(200, "{\"success\":false,\"error\":{\"code\":105}}", "Failed")]
    [InlineData(200, "unexpected", "Unknown")]
    [InlineData(500, "unavailable", "Unknown")]
    [InlineData(400, "bad request", "Failed")]
    [InlineData(302, "", "Failed")]
    public async Task ChecksApplicationResponseAndPostsEncodedPayload(int status, string response, string state)
    {
        string? posted = null;
        var handler = new Handler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("application/x-www-form-urlencoded", request.Content!.Headers.ContentType!.MediaType);
            posted = await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(response) };
        });
        using var client = new HttpClient(handler);
        var sender = new CalibrationChatSender(client, NullLogger<CalibrationChatSender>.Instance);
        var result = await sender.SendAsync(Url, "【測試】量規 A&B\n到期日", default);
        Assert.Equal(state, result.State);
        Assert.StartsWith("payload=", posted);
        using var json = JsonDocument.Parse(WebUtility.UrlDecode(posted![8..]));
        Assert.Equal("【測試】量規 A&B\n到期日", json.RootElement.GetProperty("text").GetString());
    }

    [Fact]
    public async Task TransportFailureIsUnknownAndDoesNotLeakUrl()
    {
        using var client = new HttpClient(new Handler(_ => throw new HttpRequestException(Url)));
        var result = await new CalibrationChatSender(client, NullLogger<CalibrationChatSender>.Instance).SendAsync(Url, "test", default);
        Assert.Equal("Unknown", result.State);
        Assert.DoesNotContain("secret-test-only", result.ErrorCode);
    }
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> action) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => action(request);
    }
}
