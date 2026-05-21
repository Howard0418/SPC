using NUnit.Framework;
using Microsoft.Playwright.NUnit;
using MES.SPC.E2ETests.Utilities;
using System.Net.Http.Json;

namespace MES.SPC.E2ETests.Tests;

[TestFixture]
public class SpcChartUiTests : PageTest
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
    public async Task SpcChart_Should_Load_Data_Correctly()
    {
        using var http = new HttpClient { BaseAddress = new Uri(_apiUrl) };

        // ─── 前置 1：從 API 取得第一筆有效的 PartProcessCharacteristic ───
        var ppcs = await http.GetFromJsonAsync<List<PpcDto>>("/api/part-process-characteristics");
        Assert.That(ppcs, Is.Not.Null.And.Count.GreaterThan(0),
            "資料庫應至少有一筆 PartProcessCharacteristic（請先執行 Excel 匯入）");

        var firstPpc = ppcs![0];

        // ─── 前置 2：為此 PPC 種入 25 筆範例 VariableMeasurements ───
        var seedRes = await http.PostAsync(
            $"/api/v2/migration/seed-sample-measurements?ppcId={firstPpc.Id}&count=25",
            null);
        Assert.That(seedRes.IsSuccessStatusCode, Is.True,
            $"seed-sample-measurements 應成功，狀態碼：{seedRes.StatusCode}");

        var seedJson = await seedRes.Content.ReadFromJsonAsync<SeedResponse>();
        var seedBatchId = seedJson?.BatchId;

        try
        {
            // ─── 步驟 1：登入 ───
            await Page.GotoAsync(_baseUrl + "/login");
            await Page.FillAsync("input[placeholder='帳號']", "demo");
            await Page.FillAsync("input[placeholder='密碼']", "demo123");
            await Page.ClickAsync("button:has-text('登入')");

            // ─── 步驟 2：導航至 SPC 管制圖頁面 ───
            await Page.ClickAsync("a:has-text('SPC 管制圖')");
            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/spc$"));
            await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

            // ─── 步驟 3：填入測試面板 ID（使用真實 PPC 對應的 IDs）───
            await Page.FillAsync("label:has-text('產品 ID') + input",      firstPpc.PartId.ToString());
            await Page.FillAsync("label:has-text('工站 ID') + input",      firstPpc.ProcessId.ToString());
            await Page.FillAsync("label:has-text('檢測項目 ID') + input",  firstPpc.CharacteristicId.ToString());

            // ─── 步驟 4：點擊查詢 ───
            await Page.ClickAsync("button:has-text('查詢')");

            // ─── 步驟 5：等待 API 回應 ───
            await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

            // ─── 步驟 6：確認無錯誤訊息 ───
            var errorDiv = Page.Locator("div.bg-red-500\\/10");
            var hasError = await errorDiv.IsVisibleAsync();
            if (hasError)
            {
                var errText = await errorDiv.InnerTextAsync();
                Assert.Fail($"頁面出現錯誤訊息：{errText}");
            }

            // ─── 步驟 7：驗證 ECharts canvas 已渲染 ───
            // 有 25 筆量測數據 → chartResult 不為 null → canvas 應顯示
            var canvas = Page.Locator("canvas");
            await Expect(canvas).ToBeVisibleAsync(new() { Timeout = 10000 });
        }
        finally
        {
            // ─── 清除：刪除此次種入的量測數據批次 ───
            if (!string.IsNullOrEmpty(seedBatchId))
            {
                await http.DeleteAsync(
                    $"/api/v2/migration/seed-sample-measurements?batchId={Uri.EscapeDataString(seedBatchId)}");
            }
        }
    }

    // ─── DTOs ───
    private record PpcDto(int Id, int PartId, int ProcessId, int CharacteristicId);

    private record SeedResponse(bool Success, int PpcId, string BatchId, int SeededCount, string Message);
}
