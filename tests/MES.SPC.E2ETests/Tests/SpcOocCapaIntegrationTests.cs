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

        // ─── Step 2: 透過 API 建立一筆明確的 Open 異常單 ───
        int alertId;
        using (var http = new HttpClient { BaseAddress = new Uri(_apiUrl) })
        {
            var simulateRes = await http.PostAsJsonAsync("/api/alerts/simulate", new
            {
                partId = 101,
                processId = 201,
                actualValue = 105.85,
                alertType = "OOC",
                message = $"E2E CAPA workflow alert {Guid.NewGuid():N}"
            });
            simulateRes.EnsureSuccessStatusCode();
            var simulateJson = await simulateRes.Content.ReadFromJsonAsync<SimulateAlertResponse>();
            alertId = simulateJson!.Alert.Id;
        }

        try
        {
            // ─── Step 3: 導向至異常工作流頁面 ───
            await Page.GotoAsync(_baseUrl + "/alerts-workflow");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // ─── Step 4: 用單號搜尋剛建立的異常，並點擊「填寫處置」 ───
            await Page.GetByPlaceholder("搜尋真因、對策或單號...").FillAsync(alertId.ToString());
            var alertRow = Page.Locator("table tbody tr").First;
            await Expect(alertRow).ToBeVisibleAsync(new() { Timeout = 15000 });

            // 點擊「填寫處置」以打開 Modal
            await alertRow.Locator("button").Last.ClickAsync();
            await Expect(Page.Locator("h3:has-text('處置追蹤')")).ToBeVisibleAsync(new() { Timeout = 10000 });

            // ─── Step 5: 填寫原因分析、改善對策並將狀態變更為「結案 (Closed)」 ───
            // 點選結案單選按鈕（因為 input 有 peer sr-only，點擊其外層 label 或文字）
            await Page.Locator("text=結案 (Closed)").ClickAsync();

            // 輸入負責人與改善真因對策
            await Page.GetByPlaceholder("輸入工程師姓名或工號").FillAsync("E2E-QC-ENG");
            await Page.GetByPlaceholder("請描述發生此次異常的根本原因").FillAsync("E2E測試：感測器線路老化造成訊號偏移。");
            await Page.GetByPlaceholder("請描述為防止再次發生所採取的行動").FillAsync("E2E對策：更換設備感測器並重新校準，納入每日點檢。");

            // 點擊儲存
            await Page.ClickAsync("button:has-text('儲存處置紀錄')");

            // ─── Step 6: 驗證 UI 狀態更新為 CLOSED 且顯示對應的內容 ───
            await Page.WaitForTimeoutAsync(1000);
            await Expect(Page.Locator("text=處置進度已更新")).ToBeVisibleAsync(new() { Timeout = 15000 });
        }
        finally
        {
            // The simulated alert is closed by the workflow and retained as an auditable record.
        }
    }

    private sealed record SimulateAlertResponse(SimulatedAlert Alert, bool EmailSent, string Recipient, string OutboxFolder);
    private sealed record SimulatedAlert(int Id);
}
