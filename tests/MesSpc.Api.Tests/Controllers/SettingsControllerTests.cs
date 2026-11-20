using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
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
    public void ClassifiedRecipients_KeepEmptySelectionSeparateFromLegacy()
    {
        var settings = new AlertRecipientSelection([1], [], [], []);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AlertRecipientSelection.ConfigurationKey] = System.Text.Json.JsonSerializer.Serialize(settings)
        }).Build();
        var loaded = Assert.IsType<AlertRecipientSelection>(AlertRecipientSelection.Read(config));
        Assert.Equal(new[] { 1 }, loaded.OutOfSpec);
        Assert.Empty(loaded.OutOfControl);
        Assert.Null(AlertRecipientSelection.Read(new ConfigurationBuilder().Build()));
    }

    [Fact]
    public async Task EvaluateBatch_RoutesOosAndOocToIndependentRecipients()
    {
        await using var db = CreateDb();
        db.Operators.AddRange(
            new() { Id = 1, OperatorCode = "A", OperatorName = "A", Email = "a@example.test", IsActive = true },
            new() { Id = 2, OperatorCode = "B", OperatorName = "B", Email = "b@example.test", IsActive = true });
        db.InspectionItems.Add(new() { Id = 1, Usl = 9, Ucl = 8 });
        await db.SaveChangesAsync();
        var mail = new Mock<IEmailNotificationService>();
        mail.Setup(x => x.SendAlertEmailAsync(It.IsAny<AlertEvent>(), It.IsAny<string>(), It.IsAny<string>(), null, null)).ReturnsAsync(true);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AlertRecipientSelection.ConfigurationKey] = System.Text.Json.JsonSerializer.Serialize(new AlertRecipientSelection([1], [2], [], []))
        }).Build();
        var service = new SpcService(db, mail.Object, config);

        var alerts = await service.EvaluateBatchAsync(new MeasurementBatch
        {
            Values = [new() { InspectionItemId = 1, ValueNumeric = 10 }]
        });

        Assert.Equal(2, alerts.Count);
        mail.Verify(x => x.SendAlertEmailAsync(alerts[0], "a@example.test", "A", null, null), Times.Once);
        mail.Verify(x => x.SendAlertEmailAsync(alerts[1], "b@example.test", "B", null, null), Times.Once);
        mail.VerifyNoOtherCalls();
    }

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

    [Fact]
    public async Task UpdateSmtpSettings_InvalidClassifiedRecipientLeavesFileUntouched()
    {
        await using var db = CreateDb();
        var root = Directory.CreateTempSubdirectory("spc-settings-");
        var path = Path.Combine(root.FullName, "appsettings.json");
        const string original = "{\"SmtpSettings\":{\"Host\":\"smtp.example.test\"}}";
        await File.WriteAllTextAsync(path, original);
        try
        {
            var env = new Mock<IWebHostEnvironment>();
            env.SetupGet(x => x.ContentRootPath).Returns(root.FullName);
            var controller = new SettingsController(new ConfigurationBuilder().Build(), env.Object,
                Mock.Of<IEmailNotificationService>(), NullLogger<SettingsController>.Instance, db);
            var invalid = await controller.UpdateSmtpSettings(new SmtpSettingsDto
            {
                RecipientSelection = new([12345], [], [], [])
            });
            Assert.IsType<BadRequestObjectResult>(invalid);
            Assert.Equal(original, await File.ReadAllTextAsync(path));

            var valid = await controller.UpdateSmtpSettings(new SmtpSettingsDto { RecipientSelection = new([], [], [], []) });
            Assert.IsType<OkObjectResult>(valid);
            var config = new ConfigurationBuilder().AddJsonFile(path).Build();
            var selection = Assert.IsType<AlertRecipientSelection>(AlertRecipientSelection.Read(config));
            Assert.Empty(selection.OutOfSpec);
            Assert.Empty(await db.SpcAlertNotificationSettings.ToListAsync());
        }
        finally { root.Delete(recursive: true); }
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
