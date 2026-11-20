using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;

namespace MesSpc.Api.Tests;

public sealed class ChemicalFormulaNoticeTests
{
    [Theory]
    [InlineData("{}", "{\"adjustmentAmountFormula\":\" X \"}", true, false)]
    [InlineData("{\"concentrationFormula\":\"X\"}", "{}", false, true)]
    [InlineData("{}", "{\"adjustmentAmountFormula\":\"X\",\"concentrationFormula\":\"Y\"}", true, true)]
    [InlineData("{\"concentrationFormula\":\" X \"}", "{\"concentrationFormula\":\"X\",\"adjustmentFormula\":\"Y\",\"decimalPlaces\":3}", false, false)]
    public void ClassifiesOnlyTargetFields(string previous, string current, bool adjustment, bool concentration)
    {
        Assert.Equal((adjustment, concentration), ChemicalFormulaNotice.Changes(previous, current));
    }

    [Fact]
    public async Task DispatchUsesOnlyPersistedVersionAndValidDistinctRecipients()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Operators.AddRange(
            new() { Id = 1, OperatorCode = "A", OperatorName = "A", IsActive = true, Email = "a@example.test" },
            new() { Id = 2, OperatorCode = "B", OperatorName = "B", IsActive = true, Email = "b@example.test" },
            new() { Id = 3, OperatorCode = "C", OperatorName = "C", IsActive = true, Email = "A@example.test" },
            new() { Id = 4, OperatorCode = "D", OperatorName = "D", IsActive = false, Email = "d@example.test" });
        db.ChemicalAnalysisFormulaVersions.Add(new()
        {
            Id = 10, PartProcessCharacteristicId = 12, VersionNo = 3, PreviousConfigJson = "{}",
            ConfigJson = "{\"adjustmentAmountFormula\":\"X\",\"concentrationFormula\":\"Y\"}"
        });
        await db.SaveChangesAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AlertRecipientSelection.ConfigurationKey] = JsonSerializer.Serialize(new AlertRecipientSelection([], [], [1, 3, 4], [2]))
        }).Build();
        var mail = new Mock<IEmailNotificationService>();
        mail.Setup(service => service.SendHtmlEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, null, null)).ReturnsAsync(true);
        var notice = new ChemicalFormulaNotice(db, config, mail.Object, NullLogger<ChemicalFormulaNotice>.Instance);

        await notice.SendCommittedAsync(999);
        mail.VerifyNoOtherCalls();
        await notice.SendCommittedAsync(10);
        mail.Verify(service => service.SendHtmlEmailAsync("a@example.test", "A", "【SPC】調整量公式修改", It.IsAny<string>(), null, null, null), Times.Once);
        mail.Verify(service => service.SendHtmlEmailAsync("b@example.test", "B", "【SPC】濃度公式修改", It.IsAny<string>(), null, null, null), Times.Once);
        mail.VerifyNoOtherCalls();
    }
}
