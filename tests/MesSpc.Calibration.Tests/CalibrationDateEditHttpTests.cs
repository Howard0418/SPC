using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MesSpc.Api.Services.Calibration;

namespace MesSpc.Calibration.Tests;

public class CalibrationDateEditHttpTests
{
    [Theory]
    [InlineData("qa","Editor",200)]
    [InlineData("viewer","Viewer",403)]
    [InlineData("denied","Editor",403)]
    public async Task PutChecksPermissionsAndReloadsChangedDates(string user,string role,int expected)
    {
        using var factory=new CalibrationTestEmailFactory();
        using var client=factory.Client(user,role);
        var original=(await client.GetFromJsonAsync<JsonElement>("/api/v1/instruments/1")).GetProperty("data");
        var input=new InstrumentInput(original.GetProperty("code").GetString()!,original.GetProperty("name").GetString()!,"QA",1,12,
            new(2026,9,1),new(2026,9,20),"Active",[1],true,original.GetProperty("version").GetGuid(),null);
        var response=await client.PutAsJsonAsync("/api/v1/instruments/1",input);
        Assert.Equal((HttpStatusCode)expected,response.StatusCode);
        var current=(await client.GetFromJsonAsync<JsonElement>("/api/v1/instruments/1")).GetProperty("data");
        Assert.Equal(expected==200 ? "2026-09-01" : null,current.GetProperty("lastCalibrationDate").GetString());
        if(expected==200)
        {
            var stale=await client.PutAsJsonAsync("/api/v1/instruments/1",input);
            Assert.Equal(HttpStatusCode.Conflict,stale.StatusCode);
        }
        Assert.Empty(factory.Mail.Sent);
    }
}
