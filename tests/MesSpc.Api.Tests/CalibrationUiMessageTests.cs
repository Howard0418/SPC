using MesSpc.Api.Services;
using MesSpc.Api.Services.Calibration;
using MesSpc.Api.Services.Localization;
using System.Text.Json;
using MesSpc.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
namespace MesSpc.Api.Tests;
public class CalibrationUiMessageTests
{
    [Theory]
    [InlineData(false,"spc.api.chameleonSourceUnavailable","無法連線至此 Chameleon 資料來源。")]
    [InlineData(true,"spc.api.chameleonTimestampMissing","Chameleon 未回傳資料時間。")]
    public async Task EquipmentFailureKeepsStatusRawMessageAndCacheWithoutExtraRequests(bool missingTime,string key,string message)
    {
        using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var handler=new DeviceHandler(missingTime);using var http=new HttpClient(handler);using var cache=new MemoryCache(new MemoryCacheOptions());
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Chameleon:BaseUrl","http://example.invalid"}}).Build();
        var service=new ChameleonStatusService(http,config,db,cache,NullLogger<ChameleonStatusService>.Instance);
        var first=await service.GetStatusAsync(default);var calls=handler.Calls;Assert.Same(first,await service.GetStatusAsync(default));Assert.Equal(calls,handler.Calls);
        var source=Assert.Single(first.Sources);Assert.True(first.IsConfigured);
        if(missingTime){var device=Assert.Single(source.Devices);Assert.Equal("unavailable",device.Status);Assert.Equal(message,device.Message);Assert.Equal(key,device.MessageKey);}
        else{Assert.Equal("unavailable",source.Status);Assert.Equal(message,source.Message);Assert.Equal(key,source.MessageKey);}
    }
    private sealed class DeviceHandler(bool missingTime):HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            Calls++;if(!missingTime)throw new HttpRequestException("mock only");
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(request.RequestUri!.AbsolutePath.EndsWith("/reading/equipments")?"[\"E1\"]":"{\"reading\":{}}")});
        }
    }
    [Fact]
    public void EmptyFileKeepsCalibrationStatusCodeAndMessageWithSafeDescriptor()
    {
        var ex=Assert.Throws<CalibrationException>(()=>CalibrationImportParser.Parse([],TimeProvider.System));
        Assert.Equal(400,ex.Status);Assert.Equal("VALIDATION",ex.Code);Assert.Equal("請上傳 10 MB 以內的 .xlsx 檔案。",ex.Message);
        Assert.Equal("spc.api.calibrationUploadSizeInvalid",ApiUiException.Read(ex)!.MessageKey);Assert.Empty(ApiUiException.Read(ex)!.Args);
    }
    [Fact]
    public void ExistingCalibrationConflictCodeRemainsOwnedByOriginalException()
    {
        var original=new CalibrationException("old conflict",409,"VERSION_CONFLICT");var ex=ApiUiException.Attach(original,"spc.api.calibrationVersionConflict");
        Assert.Same(original,ex);Assert.Equal(409,ex.Status);Assert.Equal("VERSION_CONFLICT",ex.Code);Assert.Equal("old conflict",ex.Message);
    }
    [Fact]
    public void NotConfiguredEquipmentKeepsRawStatusAndMessageWithAdditiveKey()
    {
        var status=ChameleonStatusResult.NotConfigured();var json=JsonSerializer.SerializeToElement(status,new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.False(status.IsConfigured);Assert.Equal("尚未設定 Chameleon API 位址。",status.Message);Assert.Equal("spc.api.chameleonNotConfigured",json.GetProperty("messageKey").GetString());Assert.Empty(json.GetProperty("args").EnumerateObject());
    }
}
