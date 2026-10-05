using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
namespace MesSpc.Calibration.Tests;
public class CalibrationDetailsHttpTests
{
    [Fact]
    public async Task CreateReturnsDetailsInListAndSingleInstrument()
    {
        using var factory=new CalibrationTestEmailFactory();using var client=factory.Client();
        var response=await client.PostAsJsonAsync("/api/v1/instruments",new {
            code="DETAILS-HTTP",name="規格測試",department="QA",custodianOperatorId=1,cycleMonths=12,
            usageStatus="Active",recipientOperatorIds=Array.Empty<int>(),includeCustodian=false,
            measurementSpecification="0~150 mm",precision="0.01 mm",remarks="第一行\n第二行",calibrationStandard="SOP-01",acceptanceCriteria="±0.02 mm"
        });
        Assert.True(response.IsSuccessStatusCode);
        var created=await response.Content.ReadFromJsonAsync<JsonElement>();
        var id=created.GetProperty("data").GetProperty("id").GetInt32();
        var single=await client.GetFromJsonAsync<JsonElement>($"/api/v1/instruments/{id}");
        var list=await client.GetFromJsonAsync<JsonElement>("/api/v1/instruments?keyword=DETAILS-HTTP");
        foreach(var item in new[]{single.GetProperty("data"),list.GetProperty("data").EnumerateArray().Single(x=>x.GetProperty("id").GetInt32()==id)})
        {
            Assert.Equal("0~150 mm",item.GetProperty("measurementSpecification").GetString());
            Assert.Equal("0.01 mm",item.GetProperty("precision").GetString());
            Assert.Equal("第一行\n第二行",item.GetProperty("remarks").GetString());
            Assert.Equal("SOP-01",item.GetProperty("calibrationStandard").GetString());
            Assert.Equal("±0.02 mm",item.GetProperty("acceptanceCriteria").GetString());
        }
    }
}
