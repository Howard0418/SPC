using MesSpc.Api.Services;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.SpcEngine.Models;
using MesSpc.Api.SpcEngine.Calculators;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MesSpc.Api.Infrastructure.Data;
public class ChemicalSequenceTests
{
    [Fact]public async Task ChartAndTrialUseSameSequence()
    {
        await using var c=new SqliteConnection("Data Source=:memory:;Foreign Keys=False");await c.OpenAsync();
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();
        db.Processes.Add(new(){Id=1,ProcessCode="N2",ProcessName="N2"});db.Machines.Add(new(){Id=1,MachineCode="N2",MachineName="N2",ProcessId=1});
        db.QualityCharacteristics.Add(new(){Id=1,CharacteristicCode="PH",CharacteristicName="PH",ControlScope="CHEM"});
        db.ControlChartTypes.Add(new(){Id=1,ChartTypeCode="I-MR",ChartTypeName="I-MR",DataCategory="Variable"});
        db.PartProcessCharacteristics.Add(new(){Id=1,MachineId=1,ProcessId=1,CharacteristicId=1,SampleSize=1,ChartTypeId=1,ControlScope="CHEM"});
        var date=new DateTime(2026,9,11);
        db.VariableMeasurements.AddRange(new VariableMeasurement{Id=3,PartProcessCharacteristicId=1,MeasuredAt=date,SamplingStage="CLOSE",SamplingPhase="MIDDLE",MeasuredValue=7},new VariableMeasurement{Id=2,PartProcessCharacteristicId=1,MeasuredAt=date,SamplingStage="CLOSE",SamplingPhase="OPEN",MeasuredValue=3},new VariableMeasurement{Id=1,PartProcessCharacteristicId=1,MeasuredAt=date.AddHours(9),SamplingStage="OPEN",SamplingPhase="OPEN",MeasuredValue=1});await db.SaveChangesAsync();
        var svc=new SpcService(db,null!,new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var chart=await svc.GetInteractiveChartAsync(1,null,date,date);Assert.NotNull(chart);
        var pts=JsonSerializer.SerializeToElement(chart.ChartData).GetProperty("points");Assert.Equal(new[]{1d,3d,7d},pts.EnumerateArray().Select(p=>p.GetProperty("value").GetDouble()));
        var trial=await svc.TrialCalculateLimitsAsync(1,date,date);Assert.NotNull(trial);
        var limits=JsonSerializer.SerializeToElement(chart.StatControlLimits).GetProperty("iControlLimitsStat");
        Assert.Equal(limits.GetProperty("ucl").GetDouble(),trial.Ucl!.Value,8);
    }
    [Fact]public void KeepsAllStagesAndOrdersMrConsistently()
    {
        var date=new DateTime(2026,9,11);
        var rows=new List<VariableMeasurement>{
            new(){Id=3,MeasuredAt=date,SamplingStage="CLOSE",SamplingPhase="MIDDLE",MeasuredValue=7},
            new(){Id=2,MeasuredAt=date,SamplingStage="CLOSE",SamplingPhase="OPEN",MeasuredValue=3},
            new(){Id=1,MeasuredAt=date.AddHours(9),SamplingStage="OPEN",SamplingPhase="OPEN",MeasuredValue=1}};
        var ordered=ChemicalStageChart.Order(rows);Assert.Equal(new long[]{1,2,3},ordered.Select(x=>x.Id));
        var result=ImrChartCalculator.Calculate(ordered.Select(x=>new SpcDataPoint{Value=x.MeasuredValue,MeasuredAt=x.MeasuredAt,SamplingPhase=x.SamplingPhase,SamplingStage=x.SamplingStage}).ToList(),new());
        var top=JsonSerializer.SerializeToElement(result.ChartData).GetProperty("points");Assert.Equal(3,top.GetArrayLength());Assert.Equal("OPEN",top[0].GetProperty("samplingStage").GetString());
        var mr=JsonSerializer.SerializeToElement(result.SecondaryChartData).GetProperty("points");Assert.Equal(2,mr[0].GetProperty("value").GetDouble());Assert.Equal(4,mr[1].GetProperty("value").GetDouble());
    }
}

