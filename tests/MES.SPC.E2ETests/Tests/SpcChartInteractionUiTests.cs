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

        var ppcs = await http.GetFromJsonAsync<List<PpcDto>>("/api/part-process-characteristics");
        Assert.That(ppcs, Is.Not.Null.And.Count.GreaterThan(0));
        var firstPpc = ppcs![0];

        var seedRes = await http.PostAsync(
            $"/api/v2/migration/seed-sample-measurements?ppcId={firstPpc.Id}&count=25",
            null);
        Assert.That(seedRes.IsSuccessStatusCode, Is.True);
        var seedJson = await seedRes.Content.ReadFromJsonAsync<SeedResponse>();
        var seedBatchId = seedJson?.BatchId;

        try
        {
            // API：確認回傳之 limits 含規格/管制界限（對應 ECharts markLine）
            var chartJson = await http.GetFromJsonAsync<JsonElement>(
                $"/api/v2/spc/interactive-chart?partProcessCharacteristicId={firstPpc.Id}");
            Assert.That(chartJson.TryGetProperty("limits", out var limits), Is.True);
            Assert.That(limits.TryGetProperty("usl", out var usl) && usl.ValueKind != JsonValueKind.Null
                || limits.TryGetProperty("ucl", out var ucl) && ucl.ValueKind != JsonValueKind.Null,
                "管制圖資料應包含 USL 或 UCL 界限");

            await E2EAuthHelper.LoginAsync(Page, _baseUrl);

            await Page.ClickAsync("a:has-text('SPC 管制圖')");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Page.FillAsync("label:has-text('產品 ID') + input", firstPpc.PartId.ToString());
            await Page.FillAsync("label:has-text('工站 ID') + input", firstPpc.ProcessId.ToString());
            await Page.FillAsync("label:has-text('檢測項目 ID') + input", firstPpc.CharacteristicId.ToString());
            await Page.ClickAsync("button:has-text('查詢')");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var chartHost = Page.Locator("div.h-\\[600px\\].w-full").First;
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
            if (!string.IsNullOrEmpty(seedBatchId))
            {
                await http.DeleteAsync(
                    $"/api/v2/migration/seed-sample-measurements?batchId={Uri.EscapeDataString(seedBatchId)}");
            }
        }
    }

    private record PpcDto(int Id, int PartId, int ProcessId, int CharacteristicId);
    private record SeedResponse(bool Success, int PpcId, string BatchId, int SeededCount, string Message);
}
