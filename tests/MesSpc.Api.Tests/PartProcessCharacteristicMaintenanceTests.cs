using FluentAssertions;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace MesSpc.Api.Tests;

public class PartProcessCharacteristicMaintenanceTests
{
    [Fact]
    public async Task Create_Should_Use_Configured_Group_FieldRequirements()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options);
        var process = new Process { ProcessCode = "CUSTOM_P", ProcessName = "自訂製程", ControlScope = "CUSTOM" };
        var characteristic = new QualityCharacteristic { CharacteristicCode = "CUSTOM_C", CharacteristicName = "自訂特性", ControlScope = "CUSTOM", DataCategory = "Variable" };
        var group = new ControlChartGroup
        {
            GroupCode = "CUSTOM_GROUP", GroupName = "自訂範圍", GroupType = "CONTROL_CHART",
            BusinessScopeCode = "CUSTOM", RequiresMachine = true, RequiresTank = true
        };
        db.AddRange(process, characteristic, group);
        await db.SaveChangesAsync();
        var chartType = new ControlChartType { ChartGroupId = group.Id, ChartTypeCode = "CUSTOM_IMR", ChartTypeName = "自訂 I-MR", DataCategory = "Variable", RequiredSampleSize = 1 };
        db.Add(chartType);
        await db.SaveChangesAsync();

        var result = await new PartProcessCharacteristicsController(db).Create(new PartProcessCharacteristic
        {
            ControlScope = "CUSTOM", ProcessId = process.Id, CharacteristicId = characteristic.Id,
            ChartTypeId = chartType.Id, SampleSize = 1
        });

        result.Should().BeOfType<BadRequestObjectResult>();
        ((BadRequestObjectResult)result).Value.Should().Be("自訂範圍管制項目必須選擇線別/機台。");
    }

    [Fact]
    public async Task Update_Should_Save_Unit_And_ItemSpecificRules()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        var process = new Process { ProcessCode = "P1", ProcessName = "製程一", ControlScope = "PROCESS" };
        var characteristic = new QualityCharacteristic
        {
            CharacteristicCode = "C1",
            CharacteristicName = "厚度",
            ControlScope = "PROCESS",
            Unit = "mm"
        };
        var chartGroup = new ControlChartGroup
        {
            GroupCode = "PROCESS",
            GroupName = "製程管制",
            GroupType = "CONTROL_CHART"
        };
        var libraryGroup = new SpcRuleGroup
        {
            RuleGroupCode = "WE",
            RuleGroupName = "規則庫"
        };
        db.AddRange(process, characteristic, libraryGroup, chartGroup);
        await db.SaveChangesAsync();

        var chartType = new ControlChartType
        {
            ChartGroupId = chartGroup.Id,
            ChartTypeCode = "XBAR_R",
            ChartTypeName = "平均數-全距圖",
            DataCategory = "Variable",
            RequiredSampleSize = 5
        };
        db.ControlChartTypes.Add(chartType);
        await db.SaveChangesAsync();

        db.SpcRules.AddRange(
            new SpcRule { RuleGroupId = libraryGroup.Id, RuleCode = "RULE_1", RuleName = "規則一", Priority = 1 },
            new SpcRule { RuleGroupId = libraryGroup.Id, RuleCode = "RULE_2", RuleName = "規則二", Priority = 2 });
        var item = new PartProcessCharacteristic
        {
            ControlScope = "PROCESS",
            ProcessId = process.Id,
            CharacteristicId = characteristic.Id,
            Unit = "μm",
            USL = 10.2,
            LSL = 9.8,
            SampleSize = 5,
            ChartTypeId = chartType.Id
        };
        db.PartProcessCharacteristics.Add(item);
        await db.SaveChangesAsync();

        var controller = new PartProcessCharacteristicsController(db);
        const string formulaConfig = """{"XbarCalculationMethod":"SIGMA_METHOD","Multiplier":3}""";
        var updateResult = await controller.Update(item.Id, new PartProcessCharacteristic
        {
            ControlScope = item.ControlScope,
            ProcessId = item.ProcessId,
            CharacteristicId = item.CharacteristicId,
            Unit = "μm",
            USL = item.USL,
            LSL = item.LSL,
            SampleSize = item.SampleSize,
            ChartTypeId = item.ChartTypeId,
            FormulaConfigJson = formulaConfig,
            IsRequired = true,
            IsEnabled = true
        });
        var rulesResult = await controller.UpdateRules(
            item.Id,
            new PartProcessCharacteristicsController.ItemRulesUpdateRequest(["RULE_1"]));

        updateResult.Should().BeOfType<OkObjectResult>();
        rulesResult.Should().BeOfType<OkObjectResult>();
        var saved = await db.PartProcessCharacteristics.SingleAsync(x => x.Id == item.Id);
        saved.Unit.Should().Be("μm");
        saved.FormulaConfigJson.Should().Be(formulaConfig);
        saved.RuleGroupId.Should().NotBeNull();
        (await db.SpcRules.SingleAsync(x => x.RuleGroupId == saved.RuleGroupId && x.RuleCode == "RULE_1"))
            .IsEnabled.Should().BeTrue();
        (await db.SpcRules.SingleAsync(x => x.RuleGroupId == saved.RuleGroupId && x.RuleCode == "RULE_2"))
            .IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Calculation_Should_Prefer_ItemRuleGroup_Over_ChartTypeRuleGroup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);
        var chartRules = new SpcRuleGroup { RuleGroupCode = "CHART", RuleGroupName = "管制圖規則" };
        var itemRules = new SpcRuleGroup { RuleGroupCode = "ITEM", RuleGroupName = "項目規則" };
        db.AddRange(chartRules, itemRules);
        await db.SaveChangesAsync();

        var chartType = new ControlChartType
        {
            ChartGroupId = 1,
            ChartTypeCode = "I_MR",
            ChartTypeName = "I-MR",
            RuleGroupId = chartRules.Id
        };
        var item = new PartProcessCharacteristic
        {
            ControlScope = "PROCESS",
            ProcessId = 1,
            CharacteristicId = 1,
            ChartTypeId = 1,
            RuleGroupId = itemRules.Id,
            SampleSize = 1
        };
        db.AddRange(chartType, item);
        await db.SaveChangesAsync();

        var measurement = new VariableMeasurement
        {
            PartProcessCharacteristicId = item.Id,
            ProcessId = 1,
            CharacteristicId = 1,
            MeasuredValue = 10,
            MeasuredAt = DateTime.UtcNow
        };
        db.VariableMeasurements.Add(measurement);
        await db.SaveChangesAsync();

        var service = new SpcService(
            db,
            Mock.Of<IEmailNotificationService>(),
            new ConfigurationBuilder().Build());

        var result = await service.CalculateVariableAsync(measurement);

        result.Should().NotBeNull();
        result!.RuleGroupId.Should().Be(itemRules.Id);
    }
}
