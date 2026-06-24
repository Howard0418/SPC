using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using MES.SPC.E2ETests.Utilities;
using NUnit.Framework;

namespace MES.SPC.E2ETests.Tests;

/// <summary>
/// test-plan 1.3 步驟 5：SPC 圖表 Zoom 與規格線顯示。
/// </summary>
[TestFixture]
public class SpcChartInteractionUiTests : PageTest
{
    private string _baseUrl = "";
    private string _apiUrl = "";

    [SetUp]
    public void Setup()
    {
        var settings = ConfigReader.Load();
        _baseUrl = settings.WebBaseUrl;
        _apiUrl = settings.ApiBaseUrl;
    }

    [Test]
    public async Task SpcChart_Should_Show_Spec_Lines_And_Support_Zoom()
    {
        using var http = new HttpClient { BaseAddress = new Uri(_apiUrl) };

        var firstPpc = await E2ESpcDataHelper.GetUsablePpcWithLimitsAsync(http);
        var seedJson = await E2ESpcDataHelper.TrySeedMeasurementsAsync(http, firstPpc.Id);

        try
        {
            // API：確認回傳之 limits 含規格/管制界限（對應 ECharts markLine）
            var chartJson = await http.GetFromJsonAsync<JsonElement>(
                $"/api/v1/spc/chart?ppcId={firstPpc.Id}");
            Assert.That(chartJson.TryGetProperty("limits", out var limits), Is.True);
            Assert.That(limits.EnumerateObject().Any(x => x.Value.ValueKind != JsonValueKind.Null),
                "管制圖資料應包含至少一個界限值");

            await E2EAuthHelper.LoginAsync(Page, _baseUrl);

            await Page.GotoAsync($"{_baseUrl}/spc?ppcId={firstPpc.Id}");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var chartHost = Page.GetByTestId("primary-spc-chart");
            await Expect(chartHost).ToBeVisibleAsync(new() { Timeout = 15000 });
            await Expect(Page.Locator("canvas").First).ToBeVisibleAsync();

            // UI：圖例說明應標示規格/管制界限
            await Expect(Page.Locator("text=規格/管制界限失控點")).ToBeVisibleAsync();

            // UI：滾輪縮放（ECharts inside dataZoom）
            var box = await chartHost.BoundingBoxAsync();
            Assert.That(box, Is.Not.Null);
            await Page.Mouse.MoveAsync(box!.X + box.Width / 2, box!.Y + box.Height / 2);
            await Page.Mouse.WheelAsync(0, -400);
            await Page.WaitForTimeoutAsync(500);
            await Expect(Page.Locator("canvas").First).ToBeVisibleAsync();

            // UI：製程能力指標區塊應隨圖表載入
            await Expect(Page.Locator("text=Cpk (製程能力指標)")).ToBeVisibleAsync();
        }
        finally
        {
            await E2ESpcDataHelper.TryClearSeedMeasurementsAsync(http, seedJson?.BatchId);
        }
    }
}
