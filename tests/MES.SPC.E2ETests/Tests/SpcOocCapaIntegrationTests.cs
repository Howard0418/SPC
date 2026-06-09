using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using MES.SPC.E2ETests.Utilities;
using NUnit.Framework;

namespace MES.SPC.E2ETests.Tests;

/// <summary>
/// 異常處置與 CAPA 整合流程測試：
/// 登入 → 匯入觸發 OOC 數據 → 前往異常工作流 → 填寫原因與對策 → 結案。
/// </summary>
[TestFixture]
public class SpcOocCapaIntegrationTests : PageTest
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
    public void GenerateTemplatesOnly()
    {
        var relativePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "sample-data"));
        Directory.CreateDirectory(relativePath);
        
        var varPath = Path.Combine(relativePath, "計量型資料匯入範本.xlsx");
        var attrPath = Path.Combine(relativePath, "計數型資料匯入範本.xlsx");
        
        E2EExcelFixtures.WriteTemplatesTo(varPath, attrPath);
        
        Assert.Pass($"Templates generated successfully in: {relativePath}");
    }

    [Test]
    public async Task Ooc_Capa_Workflow_Should_Succeed()
    {
        // ─── Step 1: 登入 ───
        await E2EAuthHelper.LoginAsync(Page, _baseUrl);
        await Expect(Page.Locator("text=製造統計品質即時監控看板")).ToBeVisibleAsync();

        // ─── Step 2: 透過 API 匯入刻意製造 OOC 異常的批次數據（Rule 1 & OOS） ───
        Guid batchId;
        var lotNo = $"L-E2E-OOC-{Guid.NewGuid():N}"[..16];
        using (var http = new HttpClient { BaseAddress = new Uri(_apiUrl) })
        {
            var rows = new List<Dictionary<string, string?>>();
            for (var i = 0; i < 30; i++)
            {
                rows.Add(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["PartNo"] = "P-1001",
                    ["ProcessCode"] = "ST-01",
                    ["MachineCode"] = "M-01",
                    ["CharacteristicCode"] = "LEN-001",
                    ["MeasuredValue"] = i == 29 ? "15.0" : "10.02", // 第 30 筆值為 15.0，大幅超出 USL=10.35 與 3σ
                    ["MeasuredAt"] = DateTime.UtcNow.AddMinutes(-i * 10).ToString("yyyy-MM-dd HH:mm:ss"),
                    ["Operator"] = "E2E-OP",
                    ["LotNo"] = lotNo,
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

        try
        {
            // ─── Step 3: 導向至異常工作流頁面 ───
            await Page.GotoAsync(_baseUrl + "/alerts-workflow");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // ─── Step 4: 搜尋我們的異常 Lot，並點擊「填寫處置」 ───
            await Page.FillAsync("placeholder='搜尋真因、對策或單號...'", lotNo);
            await Page.Keyboard.PressAsync("Enter");
            await Page.WaitForTimeoutAsync(1000);

            // 確保該異常單在列表上
            var alertRow = Page.Locator("table tbody tr").First;
            await Expect(alertRow).ToBeVisibleAsync(new() { Timeout = 15000 });
            await Expect(alertRow.Locator("text=OPEN")).ToBeVisibleAsync();

            // 點擊「填寫處置」以打開 Modal
            await alertRow.Locator("button:has-text('填寫處置')").ClickAsync();
            await Expect(Page.Locator("h3:has-text('品質異常處置與簽核')")).ToBeVisibleAsync(new() { Timeout = 10000 });

            // ─── Step 5: 填寫原因分析、改善對策並將狀態變更為「結案 (Closed)」 ───
            // 點選結案單選按鈕（因為 input 有 peer sr-only，點擊其外層 label 或文字）
            await Page.Locator("text=結案 (Closed)").ClickAsync();

            // 輸入負責人與改善真因對策
            await Page.FillAsync("placeholder='輸入工程師姓名或工號'", "E2E-QC-ENG");
            await Page.FillAsync("placeholder='請描述發生此次異常的根本原因'", "E2E測試：感測器線路老化造成訊號偏移。");
            await Page.FillAsync("placeholder='請描述為防止再次發生所採取的行動'", "E2E對策：更換設備感測器並重新校準，納入每日點檢。");

            // 點擊儲存
            await Page.ClickAsync("button:has-text('儲存處置紀錄')");

            // ─── Step 6: 驗證 UI 狀態更新為 CLOSED 且顯示對應的內容 ───
            await Page.WaitForTimeoutAsync(1000);
            await Expect(alertRow.Locator("text=CLOSED")).ToBeVisibleAsync(new() { Timeout = 15000 });
            await Expect(alertRow.Locator("text=E2E-QC-ENG")).ToBeVisibleAsync();
            await Expect(alertRow.Locator("text=E2E測試：感測器線路老化造成訊號偏移。")).ToBeVisibleAsync();
        }
        finally
        {
            // ─── 清理測試批次 ───
            try
            {
                using var http = new HttpClient { BaseAddress = new Uri(_apiUrl) };
                await http.DeleteAsync($"/api/uploads/{batchId}");
            }
            catch
            {
                // Ignore cleanup errors to avoid failing the test run itself
            }
        }
    }

    private sealed record UploadBatchResponse(Guid UploadBatchId, string ImportStatus, int TotalRows, int ValidRows, int ErrorRows);
}
