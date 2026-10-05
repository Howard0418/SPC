using System.Text.Json;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using MesSpc.Api.SpcEngine.Calculators;
using MesSpc.Api.SpcEngine.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public class EtchTests
{
    [Fact]
    public async Task ChartKeepsCompleteGroupsBeyond1000_AndDoesNotMixLegacySummary()
    {
        await using var c=new SqliteConnection("Data Source=:memory:;Foreign Keys=False");await c.OpenAsync();
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();
        db.Processes.Add(new(){Id=1,ProcessCode="PT2",ProcessName="PT2"});
        db.QualityCharacteristics.Add(new(){Id=1,CharacteristicCode="ETCH_A_AVG",CharacteristicName="A"});
        db.ControlChartTypes.Add(new(){Id=1,ChartTypeCode="XBAR_S",ChartTypeName="S"});
        db.PartProcessCharacteristics.Add(new(){Id=1,ProcessId=1,CharacteristicId=1,SampleSize=50,ChartTypeId=1,ControlScope="PROCESS"});
        for(var day=1;day<=21;day++)for(var i=1;i<=50;i++)db.VariableMeasurements.Add(new(){PartProcessCharacteristicId=1,MeasuredAt=new(2026,9,day),SampleNo=i,MeasuredValue=i,SourceReference=$"ETCH:2026-09-{day:00}:PT2"});
        db.VariableMeasurements.Add(new(){PartProcessCharacteristicId=1,MeasuredAt=new(2026,8,1),MeasuredValue=99,SourceReference="legacy"});
        await db.SaveChangesAsync();
        var svc=new SpcService(db,null!,new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var chart=await svc.GetInteractiveChartAsync(1,null);
        Assert.NotNull(chart);Assert.Null(chart.SubgroupSizeNote);
        var pts=JsonSerializer.SerializeToElement(chart.ChartData).GetProperty("points");
        Assert.Equal(21,pts.GetArrayLength());Assert.All(pts.EnumerateArray(),p=>Assert.Equal(50,p.GetProperty("n").GetInt32()));
    }
    [Fact]
    public void Constant50PointSample_HasZeroS()
    {
        var result=XbarSChartCalculator.Calculate([new(){MeasuredAt=new(2026,9,1),Values=Enumerable.Repeat(2d,50).ToList()}],new(),50);
        Assert.Equal(0,JsonSerializer.SerializeToElement(result.ChartData).GetProperty("points")[0].GetProperty("stdDev").GetDouble());
    }
    [Theory]
    [InlineData(25)] [InlineData(50)]
    public void FullSubgroup_HasSampleDeviationAndLimits(int n)
    {
        var g=new Subgroup {MeasuredAt=new(2026,9,1),Values=Enumerable.Range(1,n).Select(x=>(double)x).ToList()};
        var result=XbarSChartCalculator.Calculate([g],new(),n);
        var json=JsonSerializer.SerializeToElement(result.ChartData);
        Assert.Equal((n+1)/2d,json.GetProperty("points")[0].GetProperty("xbar").GetDouble(),8);
        Assert.Equal(Math.Sqrt(n*(n+1)/12d),json.GetProperty("points")[0].GetProperty("stdDev").GetDouble(),8);
        Assert.Null(result.SubgroupSizeNote);
        Assert.True(SpcConstants.TryGetXbarSFactors(n,out var a3,out var b3,out var b4));
        Assert.True(a3>0);Assert.True(b3>0);Assert.True(b4>1);
        if(n==50)Assert.Equal(.42643406173052373,a3,10);
    }
    [Fact]
    public void WrongSize_IsNotSilentlyUsedAsNewBaseline()
    {
        var result=XbarSChartCalculator.Calculate([new(){MeasuredAt=new(2026,9,1),Values=[1,2,3]}],new(),50);
        Assert.Equal(50,result.SubgroupSize);Assert.NotNull(result.SubgroupSizeNote);
        Assert.Equal(JsonValueKind.Null,JsonSerializer.SerializeToElement(result.StatControlLimits).GetProperty("xbarControl").ValueKind);
    }
    [Theory] [InlineData("PT1",5)] [InlineData("PT2",10)]
    public async Task Sync_IsAtomicIdempotent_PreservesAllPointsAndUpdatesIds(string line,int ops)
    {
        await using var c=new SqliteConnection("Data Source=:memory:;Foreign Keys=False");await c.OpenAsync();
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(c).Options);await db.Database.EnsureCreatedAsync();
        db.Machines.Add(new(){Id=1,MachineCode=line,MachineName=line,ProcessId=1});
        var codes=new[]{"ETCH_A_AVG","ETCH_B_AVG","ETCH_RATE"};
        for(var i=0;i<3;i++){db.QualityCharacteristics.Add(new(){Id=i+1,CharacteristicCode=codes[i],CharacteristicName=codes[i]});db.PartProcessCharacteristics.Add(new(){Id=i+1,MachineId=1,ProcessId=1,CharacteristicId=i+1,ControlScope="PROCESS",SampleSize=i<2?ops*5:1});}
        await db.SaveChangesAsync();
        var points=(from side in new[]{"A","B"} from r in Enumerable.Range(1,5) from op in Enumerable.Range(1,ops) select new EtchPoint(side,r,op,2m,1.5m)).ToList();
        var request=new EtchReportRequest(new(2026,9,1),line,1.5m,"test",points);
        var svc=new EtchReportSyncService(db);
        var preview=await svc.PreviewAsync(request);
        Assert.False(preview.Unchanged);Assert.Equal(ops*10+1,preview.PointCount);
        Assert.Equal(0,await db.VariableMeasurements.CountAsync());Assert.Equal(0,await db.UploadBatches.CountAsync());
        var first=await svc.SyncAsync(request,"tester",false);
        Assert.Equal(ops*10+1,await db.VariableMeasurements.CountAsync());
        var ids=await db.VariableMeasurements.OrderBy(x=>x.Id).Select(x=>x.Id).ToListAsync();
        var second=await svc.SyncAsync(request,"tester",false);Assert.True(second.Unchanged);Assert.Equal(first.BatchId,second.BatchId);
        Assert.True((await svc.PreviewAsync(request)).Unchanged);
        points[0]=points[0] with {AfterValue=1.4m};
        await Assert.ThrowsAsync<InvalidOperationException>(()=>svc.PreviewAsync(request));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>svc.SyncAsync(request,"tester",false));
        await svc.SyncAsync(request,"tester",true);
        Assert.Equal(ids,await db.VariableMeasurements.OrderBy(x=>x.Id).Select(x=>x.Id).ToListAsync());
        Assert.Equal(2,await db.UploadBatches.CountAsync());
        Assert.Equal(ops*5,await db.VariableMeasurements.CountAsync(x=>x.PartProcessCharacteristicId==1));
        Assert.All(await db.VariableMeasurements.ToListAsync(),x=>Assert.Null(x.PortalDailyDate));
        points[0]=points[0] with {AfterValue=3m};
        await Assert.ThrowsAsync<ArgumentException>(()=>svc.SyncAsync(request,"tester",true));
        Assert.Equal(2,await db.UploadBatches.CountAsync());
    }
}
