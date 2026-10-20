using System.Globalization;
using System.Reflection;
using FluentAssertions;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.EntityFrameworkCore;
#if PORTAL_FORMULA_COMPATIBILITY
using PortalEvaluator = PmrPortal.Api.Services.ExcelFormulaEvaluator;
#endif

namespace MesSpc.Api.Tests;

/// <summary>Characterization tests: differences are evidence, not acceptance of legacy defects as new policy.</summary>
public class FormulaTraceCompatibilityTests
{
    [Theory]
    [InlineData("20261031 23:59:59", 2026, 10, 31)]
    [InlineData("20261101 00:00:00", 2026, 11, 1)]
    public void ExistingUploadParserAcceptsNaiveDatesWithoutAssigningATimeZone(string input, int y, int m, int d)
    {
        var method = typeof(UploadService).GetMethod("TryParseMeasurementDate", BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] args = [input, null];
        method.Invoke(null, args).Should().Be(true);
        var date = (DateTime)args[1]!;
        date.Date.Should().Be(new DateTime(y, m, d));
        date.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Fact]
    public void ExistingUploadFallbackMustNotBecomeTheNewEffectiveDateContract()
    {
        var method = typeof(UploadService).GetMethod("TryDateTime", BindingFlags.NonPublic | BindingFlags.Static)!;
        var fallback = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        method.Invoke(null, ["not-a-date", fallback]).Should().Be(fallback);
    }

#if PORTAL_FORMULA_COMPATIBILITY
    [Theory]
    [InlineData("Primary*2")]
    [InlineData("Primary/3")]
    [InlineData("IF(Primary>2,10,20)")]
    [InlineData("ROUND(Primary,0)")]
    [InlineData("Primary*F!$B$3")]
    [InlineData("IFERROR(Primary/0,7)")]
    public void ScheduledEngineMatchesRealPortalEvaluator(string formula)
    {
        var cells=new Dictionary<string,decimal>(StringComparer.OrdinalIgnoreCase) { ["PRIMARY"]=2.5m,["F!B3"]=1.02m };
        var config=System.Text.Json.JsonSerializer.Serialize(new { enabled=true,concentrationFormula=formula,decimalPlaces=8 });
        var result=ChemicalFormulaCalculation.Calculate(config,"2.5",null,"[{\"CellAddress\":\"F!B3\",\"NumericValue\":1.02}]",null,null,null);
        decimal.Parse(result.Raw,CultureInfo.InvariantCulture).Should().Be(PortalEvaluator.EvaluateNumber(formula,cells));
    }
    private static async Task<decimal> Spc(string formula, Dictionary<string, decimal>? variables = null)
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var f = new ChemicalFTableVersion { VersionCode = "F-FIXTURE", IsActive = true };
        db.Add(f); await db.SaveChangesAsync();
        db.Add(new ChemicalFTableCell { VersionId = f.Id, CellAddress = "F!B3", NormalizedCellAddress = "F!B3", NumericValue = 1.02m });
        await db.SaveChangesAsync();
        return (await new ChemicalFTableService(db).EvaluateChemicalFormulaAsync(formula, variables, default)).Value;
    }

    [Theory]
    [InlineData("Primary * 2", "5", "10")]
    [InlineData("Primary + Secondary", "1.25", "3.75")]
    [InlineData("Primary * F!$B$3", "10", "10.2")]
    public async Task SharedArithmeticAndFReferenceHaveACommonBaseline(string formula, string primary, string expected)
    {
        var variables = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        { ["Primary"] = decimal.Parse(primary, CultureInfo.InvariantCulture), ["Secondary"] = 2.5m, ["F!B3"] = 1.02m };
        var target = decimal.Parse(expected, CultureInfo.InvariantCulture);
        PortalEvaluator.EvaluateNumber(formula, variables).Should().Be(target);
        (await Spc(formula, variables)).Should().Be(target);
    }

    [Fact]
    public async Task PortalNumberOutputLimitsEightDecimalsWhileSpcRetainsMore()
    {
        PortalEvaluator.EvaluateNumber("1/3", new Dictionary<string, decimal>()).Should().Be(0.33333333m);
        (await Spc("Primary/3", new() { ["Primary"] = 1m })).Should().NotBe(0.33333333m);
    }

    [Fact]
    public async Task MissingInputIsNotEquivalentAcrossEngines()
    {
        PortalEvaluator.EvaluateNumber("Primary * 2", new Dictionary<string, decimal>()).Should().Be(0m);
        Func<Task> missing = async () => await Spc("Primary * 2");
        await missing.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ExcelIfAndRoundAreNotSilentlyPortableToNCalc()
    {
        var vars = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["Primary"] = 2.5m };
        PortalEvaluator.EvaluateNumber("ROUND(Primary,0)", vars).Should().Be(3m);
        PortalEvaluator.EvaluateNumber("IF(Primary>2,10,20)", vars).Should().Be(10m);
        Func<Task> excelSyntax = async () => await Spc("IF(Primary>2,10,20)", vars);
        await excelSyntax.Should().ThrowAsync<Exception>();
    }
#else
    [Fact(Skip = "Run tools/testing/Invoke-FormulaTraceBaseline.ps1 with the real Portal evaluator source.")]
    public void CrossEngineBaselineRequiresExplicitPortalSource() { }
#endif
}
