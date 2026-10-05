using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.Services.Calibration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MesSpc.Calibration.Tests;

public sealed class CapturingEmailService : IEmailNotificationService
{
    public readonly List<(string Email, string Subject, string Body)> Sent = [];
    public bool Result { get; set; } = true;
    public Task<bool> SendAlertEmailAsync(AlertEvent alertEvent, string recipientEmail, string recipientName, SmtpSettingsOverride? overrideSettings = null) => Task.FromResult(Result);
    public Task<bool> SendTestEmailAsync(string recipientEmail, SmtpSettingsOverride? overrideSettings = null) => Task.FromResult(Result);
    public Task<bool> SendHtmlEmailAsync(string recipientEmail, string recipientName, string subject, string htmlBody, SmtpSettingsOverride? overrideSettings = null)
    {
        Sent.Add((recipientEmail, subject, htmlBody));
        return Task.FromResult(Result);
    }
    public Task<bool> SendReportEmailAsync(string recipientEmail, string recipientName, string subject, string htmlBody, byte[] excelBytes, string fileName) => Task.FromResult(Result);
}

public sealed class CalibrationTestEmailFactory : WebApplicationFactory<InstrumentCalibrationsController>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private const string TestKey = "isolated-calibration-test-email-signing-key-only";
    public CapturingEmailService Mail { get; } = new();
    public CapturingChat Chat { get; } = new();
    public CalibrationTestEmailFactory() => connection.Open();
    protected override IHostBuilder CreateHostBuilder() => Host.CreateDefaultBuilder().ConfigureLogging(logging => logging.ClearProviders()).ConfigureWebHost(web =>
    {
        web.UseSetting("TEST_CONTENTROOT_MESSPC_API", AppContext.BaseDirectory);
        web.ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IEmailNotificationService>(Mail);
            services.AddSingleton<ICalibrationChatSender>(Chat);
            services.AddSingleton<CalibrationChatSecrets>();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
            services.AddScoped<InstrumentCalibrationService>();
            services.AddScoped<CalibrationCertificates>();
            services.AddControllers().AddApplicationPart(typeof(InstrumentCalibrationsController).Assembly);
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new()
            {
                ValidateIssuer = false, ValidateAudience = false, ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey))
            });
            services.AddAuthorization();
        });
        web.Configure(app =>
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
                db.Operators.AddRange(
                    new Operator { Id = 1, Username = "qa", OperatorCode = "QA", OperatorName = "QA", Role = "Editor", IsActive = true, Email = "qa@example.invalid" },
                    new Operator { Id = 2, Username = "viewer", OperatorCode = "VIEW", OperatorName = "VIEW", Role = "Viewer", IsActive = true },
                    new Operator { Id = 3, Username = "denied", OperatorCode = "DENIED", OperatorName = "DENIED", Role = "Editor", PagePermissionsJson = "[\"spc\"]", IsActive = true });
                db.Add(new CalibrationInstrument
                {
                    Code = "I-TEST", Name = "測試量規", Department = "QA", CustodianOperatorId = 1,
                    NextCalibrationDate = new DateOnly(2026, 9, 20), RecipientOperatorIdsJson = "[1]"
                });
                db.SaveChanges();
            }
            app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseEndpoints(e => e.MapControllers());
        });
    });
    public HttpClient Client(string? name = "qa", string role = "Editor")
    {
        var client = CreateClient();
        if (name is not null)
        {
            var jwt = new JwtSecurityToken(claims: [new(ClaimTypes.Name, name), new(ClaimTypes.Role, role)], expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)), SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        }
        return client;
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}

public class CalibrationTestEmailHttpTests
{
    private const string Endpoint = "/api/v1/instrument-calibrations/test-email";

    [Theory]
    [InlineData(null, "Editor", 401)]
    [InlineData("viewer", "Viewer", 403)]
    [InlineData("denied", "Editor", 403)]
    public async Task TestEmailRejectsUnauthorizedUsers(string? name, string role, int expected)
    {
        using var factory = new CalibrationTestEmailFactory();
        using var client = factory.Client(name, role);
        var response = await client.PostAsJsonAsync(Endpoint, new { email = "qa@example.invalid" });
        Assert.Equal((HttpStatusCode)expected, response.StatusCode);
        Assert.Empty(factory.Mail.Sent);
    }

    [Fact]
    public async Task TestEmailRejectsInvalidAddress()
    {
        using var factory = new CalibrationTestEmailFactory();
        using var client = factory.Client();
        var response = await client.PostAsJsonAsync(Endpoint, new { email = "not-an-email" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Mail.Sent);
    }

    [Fact]
    public async Task EditorCanSendTestEmailWithoutWritingNotificationRows()
    {
        using var factory = new CalibrationTestEmailFactory();
        using var client = factory.Client();
        var response = await client.PostAsJsonAsync(Endpoint, new { email = "qa@example.invalid" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.Equal("qa@example.invalid", json.GetProperty("data").GetProperty("recipient").GetString());
        Assert.Equal("I-TEST", json.GetProperty("data").GetProperty("instrumentCode").GetString());
        Assert.Single(factory.Mail.Sent);
        Assert.Equal("qa@example.invalid", factory.Mail.Sent[0].Email);
        Assert.Contains("【測試】", factory.Mail.Sent[0].Subject);
        Assert.Contains("I-TEST", factory.Mail.Sent[0].Subject);
        Assert.Contains("非正式到期通知", factory.Mail.Sent[0].Body);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await db.Set<CalibrationNotification>().CountAsync());
    }
}

public sealed class CapturingChat : ICalibrationChatSender
{
    public List<string> Sent { get; } = [];
    public Task<CalibrationMailResult> SendAsync(string url, string text, CancellationToken ct)
    { Sent.Add(text); return Task.FromResult(new CalibrationMailResult("Sent", null)); }
}
