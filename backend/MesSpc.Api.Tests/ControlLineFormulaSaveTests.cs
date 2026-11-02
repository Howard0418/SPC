using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MesSpc.Api.Tests;

public class ControlLineFormulaSaveTests
{
    private const string Before = "{\"XbarCalculationMethod\":\"MR_METHOD\",\"MrMultiplier\":3}";
    private const string After = "{\"XbarCalculationMethod\":\"MR_METHOD\",\"MrMultiplier\":2}";
    private static readonly DateTime Day = new(2026, 10, 27);
    private static PartProcessCharacteristic Request(string formula) => new()
    {
        ControlScope = "PROCESS", ProcessId = 1, CharacteristicId = 1, ChartTypeId = 1,
        SampleSize = 2, FormulaConfigJson = formula, LSL = -10, USL = 20
    };
    private static async Task<AppDbContext> Setup(SqliteConnection connection)
    {
        await connection.OpenAsync();
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        db.Processes.Add(new() { Id = 1, ProcessCode = "TEST", ProcessName = "TEST", ControlScope = "PROCESS" });
        db.QualityCharacteristics.Add(new() { Id = 1, CharacteristicCode = "TEST", CharacteristicName = "TEST", ControlScope = "PROCESS" });
        db.ControlChartGroups.Add(new() { Id = 1, GroupCode = "PROC", GroupName = "Process", BusinessScopeCode = "PROCESS" });
        db.ControlChartTypes.Add(new() { Id = 1, ChartGroupId = 1, ChartTypeCode = "XBAR_R", ChartTypeName = "Xbar R" });
        var mapping = Request(Before); mapping.Id = 1; db.PartProcessCharacteristics.Add(mapping);
        for (var group = 0; group < 3; group++)
        {
            var mean = new[] { 1d, 2d, 4d }[group];
            for (var sample = 1; sample <= 2; sample++)
                db.VariableMeasurements.Add(new() { Id = group * 2 + sample, PartProcessCharacteristicId = 1,
                    ProcessId = 1, CharacteristicId = 1, MeasuredAt = Day.AddMinutes(group),
                    MeasuredValue = mean + (sample == 1 ? -.5 : .5), SampleNo = sample });
        }
        db.SpcCalculationResults.AddRange(new SpcCalculationResult { Id = 1, PartProcessCharacteristicId = 1, ChartTypeId = 1, VariableMeasurementId = 1 },
            new SpcCalculationResult { Id = 2, PartProcessCharacteristicId = 99, ChartTypeId = 1 });
        db.ControlLimitSegments.Add(new() { Id = 1, PartProcessCharacteristicId = 1, StartDate = Day.AddYears(-1), EndDate = Day.AddMonths(-1), UCL = 100, CL = 50, LCL = 0 });
        await db.SaveChangesAsync();
        return db;
    }
    private static PartProcessCharacteristicsController Controller(AppDbContext db) => new(db)
    { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };

    [Fact]
    public async Task SaveChangedFormula_InvalidatesOnlyOwnCache_AndChartUsesNewMultiplier()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        await using var db = await Setup(connection);
        var service = new SpcService(db, null!, new ConfigurationBuilder().Build());
        var before = await service.GetInteractiveChartAsync(1, null, Day, Day);
        var beforeUcl = JsonSerializer.SerializeToElement(before!.StatControlLimits).GetProperty("xbarControl").GetProperty("ucl").GetDouble();
        Assert.Equal(41d / 6, beforeUcl, 8);
        Assert.IsType<OkObjectResult>(await Controller(db).Update(1, Request(After)));
        Assert.Equal(new[] { 99 }, await db.SpcCalculationResults.Select(x => x.PartProcessCharacteristicId).ToArrayAsync());
        db.ChangeTracker.Clear();
        var after = await service.GetInteractiveChartAsync(1, null, Day, Day);
        var afterUcl = JsonSerializer.SerializeToElement(after!.StatControlLimits).GetProperty("xbarControl").GetProperty("ucl").GetDouble();
        Assert.Equal(16d / 3, afterUcl, 8);
        Assert.NotEqual(beforeUcl, afterUcl);
        Assert.Equal(6, await db.VariableMeasurements.CountAsync());
        var segment = await db.ControlLimitSegments.SingleAsync();
        Assert.Equal(100, segment.UCL); Assert.Equal(50, segment.CL); Assert.Equal(0, segment.LCL);
        Assert.Equal(-10, (await db.PartProcessCharacteristics.SingleAsync()).LSL);
    }

    [Theory]
    [InlineData("{ \"MrMultiplier\": 3, \"XbarCalculationMethod\": \"MR_METHOD\" }", true)]
    [InlineData("{bad json", false)]
    public async Task EquivalentOrInvalidConfiguration_PreservesSavedCalculations(string formula, bool valid)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
        await using var db = await Setup(connection);
        if (valid)
        {
            var mapping = await db.PartProcessCharacteristics.SingleAsync();
            mapping.UCL = 100; mapping.CL = 50; mapping.LCL = 0;
            await db.SaveChangesAsync();
        }
        var request = Request(formula);
        if (valid) { request.UCL = 100; request.CL = 50; request.LCL = 0; }
        var result = await Controller(db).Update(1, request);
        if (valid) Assert.IsType<OkObjectResult>(result); else Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(2, await db.SpcCalculationResults.CountAsync());
        Assert.Equal(6, await db.VariableMeasurements.CountAsync());
        if (!valid) Assert.Equal(Before, (await db.PartProcessCharacteristics.SingleAsync()).FormulaConfigJson);
        else
        {
            var mapping = await db.PartProcessCharacteristics.SingleAsync();
            Assert.Equal(100, mapping.UCL); Assert.Equal(50, mapping.CL); Assert.Equal(0, mapping.LCL);
        }
    }

    [Theory]
    [InlineData(null, " ", false)]
    [InlineData("{\"nested\":{\"b\":2,\"a\":1}}", "{\"nested\":{\"a\":1,\"b\":2}}", false)]
    [InlineData("{\"n\":3}", "{\"n\":3.0}", false)]
    [InlineData("{\"a\":[1,2]}", "{\"a\":[2,1]}", true)]
    [InlineData("{\"formula\":\"Primary*2\"}", "{\"formula\":\"Primary*3\"}", true)]
    public void ComparisonPreservesMeaningfulDifferences(string? previous, string? next, bool changed) =>
        Assert.Equal(changed, PartProcessCharacteristicsController.HasFormulaConfigChanged(previous, next));
}
