using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services.Calibration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MesSpc.Calibration.Tests;
public class CalibrationChatHttpTests
{
    private const string Settings = "/api/v1/instrument-calibrations/settings";
    [Fact]
    public async Task SettingsEncryptPreserveAndNeverReturnSecrets()
    {
        using var factory = new CalibrationTestEmailFactory(); using var client = factory.Client();
        var response = await client.PutAsJsonAsync(Settings, new { reminderDays = new[] {30,7,0}, isEnabled = false, notificationChannel = "SynologyChat", chatWebhookUrl = CalibrationChatTests.Url });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("secret-test-only", await response.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.Set<CalibrationNotificationSetting>().AsNoTracking().SingleAsync();
        Assert.NotNull(row.ChatWebhookProtected);
        Assert.DoesNotContain("secret-test-only", row.ChatWebhookProtected);
        var json = await client.GetStringAsync(Settings);
        Assert.DoesNotContain(row.ChatWebhookProtected, json);
        Assert.DoesNotContain("secret-test-only", json);
        response = await client.PutAsJsonAsync(Settings, new { reminderDays = new[] {7,0}, isEnabled = false, notificationChannel = "Email", chatWebhookUrl = "" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(row.ChatWebhookProtected, (await db.Set<CalibrationNotificationSetting>().AsNoTracking().SingleAsync()).ChatWebhookProtected);
        Assert.DoesNotContain("secret-test-only", string.Join("", await db.Set<CalibrationAuditLog>().Select(x=>x.AfterJson).ToListAsync()));
    }
    [Theory]
    [InlineData("Invalid", null)]
    [InlineData("SynologyChat", null)]
    [InlineData("SynologyChat", "https://example.invalid/")]
    public async Task InvalidSettingsRejected(string channel, string? url)
    {
        using var factory = new CalibrationTestEmailFactory(); using var client = factory.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(Settings,
            new { reminderDays = new[] {7}, isEnabled = false, notificationChannel = channel, chatWebhookUrl = url })).StatusCode);
    }
    [Theory]
    [InlineData(null, "Editor", 401)]
    [InlineData("viewer", "Viewer", 403)]
    [InlineData("denied", "Editor", 403)]
    public async Task ChatTestRequiresPermission(string? user, string role, int status)
    {
        using var factory = new CalibrationTestEmailFactory(); using var client = factory.Client(user, role);
        Assert.Equal((HttpStatusCode)status, (await client.PostAsJsonAsync("/api/v1/instrument-calibrations/test-chat", new { chatWebhookUrl = CalibrationChatTests.Url })).StatusCode);
        Assert.Empty(factory.Chat.Sent);
    }
    [Fact]
    public async Task ChatTestUsesDraftThenSavedWithoutEnablingOrQueuing()
    {
        using var factory = new CalibrationTestEmailFactory(); using var client = factory.Client();
        var response = await client.PostAsJsonAsync("/api/v1/instrument-calibrations/test-chat", new { chatWebhookUrl = CalibrationChatTests.Url });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("【測試】", Assert.Single(factory.Chat.Sent));
        await client.PutAsJsonAsync(Settings, new { reminderDays = new[] {7}, isEnabled = false, notificationChannel = "SynologyChat", chatWebhookUrl = CalibrationChatTests.Url });
        response = await client.PostAsJsonAsync("/api/v1/instrument-calibrations/test-chat", new { });
        Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("success").GetBoolean());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False((await db.Set<CalibrationNotificationSetting>().SingleAsync()).IsEnabled);
        Assert.Empty(await db.Set<CalibrationNotification>().ToListAsync());
        Assert.Empty(factory.Mail.Sent);
    }
}
