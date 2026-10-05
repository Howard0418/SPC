using MesSpc.Api.Controllers;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesSpc.Api.Tests.Controllers;

public sealed class SettingsControllerTests
{
    [Fact]
    public async Task GetSmtpSettings_DoesNotReturnConfiguredPassword()
    {
        await using var db = CreateDb();
        var controller = CreateController(db, new Dictionary<string, string?>
        {
            ["SmtpSettings:Password"] = "secret-that-must-not-leak",
            ["SmtpSettings:Host"] = "smtp.example.test"
        });

        var result = await controller.GetSmtpSettings();
        var ok = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);

        Assert.DoesNotContain("secret-that-must-not-leak", json);
    }

    [Fact]
    public async Task UpdateSmtpSettings_BlankPasswordPreservesExistingPassword()
    {
        await using var db = CreateDb();
        var root = Path.Combine(Path.GetTempPath(), $"spc-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "appsettings.json"),
            "{\"SmtpSettings\":{\"Password\":\"existing-secret\"}}");
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(x => x.ContentRootPath).Returns(root);
        var controller = new SettingsController(new ConfigurationBuilder().Build(), env.Object,
            Mock.Of<IEmailNotificationService>(), NullLogger<SettingsController>.Instance, db);

        var result = await controller.UpdateSmtpSettings(new SmtpSettingsDto { Password = "" });
        Assert.IsType<OkObjectResult>(result);
        var json = await File.ReadAllTextAsync(Path.Combine(root, "appsettings.json"));

        Assert.Contains("existing-secret", json);
    }

    private static SettingsController CreateController(AppDbContext db, IDictionary<string, string?> values)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());
        return new SettingsController(config, env.Object, Mock.Of<IEmailNotificationService>(),
            NullLogger<SettingsController>.Instance, db);
    }

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"settings-{Guid.NewGuid():N}").Options);
}
