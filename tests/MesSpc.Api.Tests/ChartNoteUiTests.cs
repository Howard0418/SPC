using System.Globalization;
using System.Text.Json;
using MesSpc.Api.Services;
using MesSpc.Api.SpcEngine.Calculators;
using MesSpc.Api.SpcEngine.Models;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace MesSpc.Api.Tests;

public class ChartNoteUiTests
{
    [Fact]
    public async Task TrialSuccessAndEmptyResultPreserveOriginalLimitsCountAndNote()
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var raw="原圖表 <b>@:key {type}</b>";
        db.QualityCharacteristics.Add(new QualityCharacteristic {Id=1,CharacteristicCode="CH0",CharacteristicName="Raw",DataCategory="Variable"});
        db.ControlChartTypes.Add(new ControlChartType {Id=1,ChartTypeCode="I_MR",ChartTypeName=raw,DataCategory="Variable",IsEnabled=true});
        db.PartProcessCharacteristics.Add(new PartProcessCharacteristic {Id=1,CharacteristicId=1,ChartTypeId=1,IsEnabled=true,USL=4,LSL=0});
        await db.SaveChangesAsync();var date=new DateTime(2026,10,26);
        var service=new SpcService(db,Mock.Of<IEmailNotificationService>(),new ConfigurationBuilder().Build());
        var empty=await service.TrialCalculateLimitsAsync(1,date,date);
        Assert.Equal(0,empty!.SampleCount);Assert.Null(empty.Ucl);Assert.Equal("在此區間內無量測數據。",empty.Note);
        Assert.Equal("spc.api.trialNoData",empty.NoteMessage!.MessageKey);
        db.VariableMeasurements.AddRange(new VariableMeasurement {PartProcessCharacteristicId=1,MeasuredValue=1,MeasuredAt=date},new VariableMeasurement {PartProcessCharacteristicId=1,MeasuredValue=2,MeasuredAt=date.AddHours(1)});
        await db.SaveChangesAsync();var result=await service.TrialCalculateLimitsAsync(1,date,date);
        Assert.Equal(2,result!.SampleCount);Assert.Equal("spc.api.trialCalculated",result.NoteMessage!.MessageKey);
        Assert.Equal(raw,result.NoteMessage.Args["type"]);Assert.Equal("2",result.NoteMessage.Args["count"]);
        Assert.Equal($"試算成功。採用圖表類型: {raw}，基準子組數/樣本數: 2。",result.Note);
        Assert.Equal(1.5,result.Cl);Assert.True(result.Ucl>result.Cl);Assert.True(result.Lcl<result.Cl);
        Assert.Equal(2,await db.VariableMeasurements.CountAsync());Assert.Empty(await db.SpcCalculationResults.ToListAsync());
    }
    [Fact]
    public void DisplayDescriptorCopiesArgumentsWithoutFormattingOrChangingCulture()
    {
        var args=new Dictionary<string,string>{{"name","<b>@:key {name}</b>"},{"count","0"}};
        var culture=CultureInfo.CurrentCulture;var item=new UiDisplayMessage("spc.api.fixture",args);args["count"]="changed";
        Assert.Equal("0",item.Args["count"]);Assert.Equal("<b>@:key {name}</b>",item.Args["name"]);Assert.Same(culture,CultureInfo.CurrentCulture);
    }

    [Theory]
    [InlineData("R")][InlineData("S")]
    public void AllExcludedSubgroupsKeepOriginalPointsAndNoEstimatedLimits(string kind)
    {
        var groups=new List<Subgroup>{new(){Values=[1,2,3],IsExcluded=true,MeasuredAt=new(2026,10,26)}};
        var limits=new ControlLimits {USL=4,LSL=0};
        var result=kind=="R"?XbarRChartCalculator.Calculate(groups,limits,3):XbarSChartCalculator.Calculate(groups,limits,3);
        Assert.Equal("全部子組皆已標記為不列入計算，管制線未重新估算。",result.SubgroupSizeNote);
        Assert.Equal("spc.api.chartAllSubgroupsExcluded",result.SubgroupSizeNoteMessage!.MessageKey);
        Assert.Same(limits,result.Limits);Assert.Null(result.Capability);Assert.Equal(3,result.SubgroupSize);
        var json=JsonSerializer.SerializeToElement(result.ChartData);Assert.Single(json.GetProperty("points").EnumerateArray());
        Assert.True(json.GetProperty("points")[0].GetProperty("isExcluded").GetBoolean());
    }

    [Theory]
    [InlineData(50,"spc.api.chartSubgroupIncomplete")]
    [InlineData(1,"spc.api.chartXbarSFactorsUnavailable")]
    public void XbarSValidationNotesRetainOriginalDataAndDoNotEstimate(int expected,string key)
    {
        var group=new Subgroup {Values=expected==1?[1d]:[1d,2d,3d]};
        var result=XbarSChartCalculator.Calculate([group],new(),expected);
        Assert.Equal(key,result.SubgroupSizeNoteMessage!.MessageKey);Assert.NotEmpty(result.SubgroupSizeNote!);Assert.Null(result.Capability);
        Assert.Equal(expected,result.SubgroupSize);
    }

    [Fact]
    public void NormalityMethodAndZeroVarianceNotesDoNotChangeExistingStatistics()
    {
        var points=Enumerable.Repeat(2d,5).Select(value=>new SpcDataPoint {Value=value}).ToList();
        var result=SpcService.PopulateNormalityAndCurve(new(),points,new());
        Assert.Equal("Anderson-Darling",result.Normality!.TestName);Assert.Null(result.Normality.PValue);Assert.True(result.Normality.IsNormal);
        Assert.Equal(0,result.Normality.Statistic);Assert.Equal(0,result.Normality.Skewness);Assert.Equal(0,result.Normality.Kurtosis);
        Assert.Equal("spc.api.normalityNoVariation",result.Normality.NoteMessage!.MessageKey);
        Assert.Equal("spc.api.normalityAndersonDarlingNote",result.Normality.MethodNoteMessage!.MessageKey);
        Assert.Contains("標準差為 0",result.Normality.Note);
        var empty=JsonSerializer.Serialize(new ControlChartResult());Assert.DoesNotContain("SubgroupSizeNoteMessage",empty);
    }
}
