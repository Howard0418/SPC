using NUnit.Framework;
using Microsoft.Playwright.NUnit;
using MES.SPC.E2ETests.Utilities;

namespace MES.SPC.E2ETests.Tests;

[TestFixture]
public class SpcChartUiTests : PageTest
{
    private string _baseUrl = "";

    [SetUp]
    public void Setup()
    {
        var settings = ConfigReader.Load();
        _baseUrl = settings.WebBaseUrl;
    }

    [Test]
    public async Task SpcChart_Should_Load_Data_Correctly()
    {
        // 1. 登入
        await Page.GotoAsync(_baseUrl + "/login");
        await Page.FillAsync("input[placeholder='帳號']", "demo");
        await Page.FillAsync("input[placeholder='密碼']", "demo123");
        await Page.ClickAsync("button:has-text('登入')");

        // 2. 導航至 SPC 管制圖頁面
        await Page.ClickAsync("a:has-text('SPC 管制圖')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/spc$"));

        // 3. 輸入測試資料 (Item ID: 1, Product ID: 1, Station ID: 1)
        await Page.FillAsync("label:has-text('檢測項目 ID') + input", "1");
        await Page.FillAsync("label:has-text('產品 ID') + input", "1");
        await Page.FillAsync("label:has-text('工站 ID') + input", "1");

        // 4. 點擊查詢
        await Page.ClickAsync("button:has-text('查詢')");

        // 5. 驗證圖表是否渲染 (檢查 Canvas 是否存在且不為空)
        var canvas = Page.Locator("canvas");
        await Expect(canvas).ToBeVisibleAsync();
        
        // 6. 檢查是否有錯誤訊息
        var errorMsg = Page.Locator("p.text-red-600");
        await Expect(errorMsg).Not.ToBeVisibleAsync();
    }
}
