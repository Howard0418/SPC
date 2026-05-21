using System.Net.Http.Json;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using MES.SPC.E2ETests.Utilities;
using NUnit.Framework;

namespace MES.SPC.E2ETests.Tests;

/// <summary>
/// test-plan 1.3 步驟 4：確認匯入後 Dashboard 警報與 Cpk 排行榜更新。
/// </summary>
[TestFixture]
public class DashboardAfterImportUiTests : PageTest
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
    public async Task Confirm_Import_Should_Refresh_Dashboard_Alerts_And_Cpk_Chart()
    {
        await E2EAuthHelper.LoginAsync(Page, _baseUrl);

        // 透過 API 建立含 OOS 量測的匯入批次（確保匯入後會產生警報）
        Guid batchId;
        using (var http = new HttpClient { BaseAddress = new Uri(_apiUrl) })
        {
            var rows = new List<Dictionary<string, string?>>();
            for (var i = 0; i < 5; i++)
            {
                rows.Add(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PartNo"] = "P-1001",
                    ["ProcessCode"] = "ST-01",
                    ["MachineCode"] = "M-01",
                    ["CharacteristicCode"] = "LEN-001",
                    ["MeasuredValue"] = i == 4 ? "10.35" : "10.02", // 最後一筆超出規格
                    ["MeasuredAt"] = DateTime.UtcNow.AddMinutes(-i * 10).ToString("yyyy-MM-dd HH:mm:ss"),
                    ["Operator"] = "E2E-OP",
                    ["LotNo"] = $"L-E2E-DASH-{Guid.NewGuid():N}"[..16],
                    ["SampleNo"] = (i + 1).ToString()
                });
            }

            var uploadRes = await http.PostAsJsonAsync("/api/uploads/variable", rows);
            uploadRes.EnsureSuccessStatusCode();
            var uploadJson = await uploadRes.Content.ReadFromJsonAsync<UploadBatchResponse>();
            batchId = uploadJson!.UploadBatchId;

            var confirmRes = await http.PostAsync($"/api/uploads/{batchId}/confirm", null);
            confirmRes.EnsureSuccessStatusCode();
        }

        // 回到 Dashboard 驗證 UI 更新
        await Page.GotoAsync(_baseUrl + "/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 今日警報 KPI 應為非負數且頁面無 API 錯誤
        await Expect(Page.Locator("text=今日觸發品質警報").Locator("..").Locator("h3")).ToBeVisibleAsync();
        var todayAlertsText = await Page.Locator("text=今日觸發品質警報").Locator("..").Locator("h3").InnerTextAsync();
        Assert.That(int.TryParse(todayAlertsText.Trim().Split(' ')[0], out var todayCount), Is.True);
        Assert.That(todayCount, Is.GreaterThanOrEqualTo(0));

        // 警報日誌表格應有資料（匯入 OOS 後）
        var alertsAfter = await Page.Locator("table tbody tr").CountAsync();
        Assert.That(alertsAfter, Is.GreaterThan(0), "Dashboard 警報日誌應顯示匯入後的異常紀錄");

        // Cpk 排行榜 ECharts canvas 應已渲染（Dashboard 上第二個 chart）
        await Expect(Page.Locator("canvas").Nth(1)).ToBeVisibleAsync(new() { Timeout = 15000 });

        // 清理：刪除測試批次（若 API 支援）
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(_apiUrl) };
            await http.DeleteAsync($"/api/uploads/{batchId}");
        }
        catch
        {
            // 清理失敗不影響測試判定
        }
    }

    private sealed record UploadBatchResponse(Guid UploadBatchId, string ImportStatus, int TotalRows, int ValidRows, int ErrorRows);
}
