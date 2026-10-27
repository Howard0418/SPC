using System.Text.Json;
using MesSpc.Api.Controllers;
using MesSpc.Api.Domain.Entities;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
namespace MesSpc.Api.Tests;
public class CharacteristicNameProjectionTests
{
    private const string Chinese="比重", English="Proportion <b>@:key {name}</b>";
    [Fact]
    public async Task AlertAndVariableAttributeTraceKeepLegacyIdentityValuesAndAddEnglishName()
    {
        using var db=Create();Seed(db);var batch=Guid.NewGuid();
        db.VariableMeasurements.Add(new VariableMeasurement {Id=1,UploadBatchId=batch,PartProcessCharacteristicId=1,ProcessId=1,CharacteristicId=1,MeasuredValue=0,WorkOrderNo="WO0",MeasuredAt=new(2026,10,27)});
        db.AttributeMeasurements.Add(new AttributeMeasurement {Id=2,UploadBatchId=batch,PartProcessCharacteristicId=1,ProcessId=1,CharacteristicId=1,InspectedQty=10,DefectQty=0,WorkOrderNo="WO0",MeasuredAt=new(2026,10,27)});
        db.AlertEvents.Add(new AlertEvent {Id=7,PartId=0,ProcessId=1,CharacteristicId=1,ActualValue=0,Message=English,Status="Open"});await db.SaveChangesAsync();
        var alerts=new AlertsController(db,Mock.Of<IEmailNotificationService>(),new ConfigurationBuilder().Build());
        var alert=Json(Assert.IsType<OkObjectResult>(await alerts.Get(null,null,null)).Value)[0];
        Assert.Equal(7,alert.GetProperty("id").GetInt32());Assert.Equal("CH0",alert.GetProperty("characteristicCode").GetString());Assert.Equal(Chinese,alert.GetProperty("characteristicName").GetString());Assert.Equal(English,alert.GetProperty("characteristicNameEn").GetString());Assert.Equal(English,alert.GetProperty("message").GetString());Assert.Equal(0,alert.GetProperty("actualValue").GetDouble());
        var trace=Json(Assert.IsType<OkObjectResult>(await new TraceabilityV1Controller(db).Query("WO0",null,null)).Value);
        var rows=trace.GetProperty("batches").EnumerateArray().ToList();Assert.Equal(2,rows.Count);
        foreach(var row in rows){Assert.Equal(Chinese,row.GetProperty("characteristicName").GetString());Assert.Equal(English,row.GetProperty("characteristicNameEn").GetString());Assert.Equal("WO0",row.GetProperty("workOrderNo").GetString());Assert.Equal(1,row.GetProperty("ppcId").GetInt32());Assert.Single(row.GetProperty("values").EnumerateArray());}
        Assert.Equal(1,await db.VariableMeasurements.CountAsync());Assert.Equal(1,await db.AttributeMeasurements.CountAsync());Assert.Empty(await db.SpcCalculationResults.ToListAsync());
    }
    [Fact]
    public async Task MonitorContextAddsEnglishWithoutChangingRawNamesLimitsOrPoints()
    {
        using var db=Create();Seed(db);db.VariableMeasurements.AddRange(new VariableMeasurement {Id=1,PartProcessCharacteristicId=1,MeasuredValue=1,MeasuredAt=new(2026,10,27)},new VariableMeasurement {Id=2,PartProcessCharacteristicId=1,MeasuredValue=2,MeasuredAt=new(2026,10,27,1,0,0)});await db.SaveChangesAsync();
        var service=new SpcService(db,Mock.Of<IEmailNotificationService>(),new ConfigurationBuilder().Build());
        var result=await service.GetInteractiveChartAsync(1,null);Assert.NotNull(result);
        Assert.Equal(Chinese,result.MonitorContext!.CharacteristicName);Assert.Equal(English,result.MonitorContext.CharacteristicNameEn);Assert.Equal("CH0",result.MonitorContext.CharacteristicCode);Assert.Equal(4,result.Limits!.USL);Assert.Equal(0,result.Limits.LSL);Assert.Equal(2,Json(result.RawDataPoints).GetArrayLength());Assert.Empty(await db.SpcCalculationResults.ToListAsync());
    }
    private static AppDbContext Create()=>new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static JsonElement Json(object? value)=>JsonSerializer.SerializeToElement(value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
    private static void Seed(AppDbContext db){db.Processes.Add(new Process {Id=1,ProcessCode="P0",ProcessName="原工站"});db.QualityCharacteristics.Add(new QualityCharacteristic {Id=1,CharacteristicCode="CH0",CharacteristicName=Chinese,CharacteristicNameEn=English,DataCategory="Variable"});db.ControlChartTypes.Add(new ControlChartType {Id=1,ChartTypeCode="I_MR",ChartTypeName="Original chart title",DataCategory="Variable"});db.PartProcessCharacteristics.Add(new PartProcessCharacteristic {Id=1,ProcessId=1,CharacteristicId=1,ChartTypeId=1,IsEnabled=true,USL=4,LSL=0,SampleSize=1});}
}
