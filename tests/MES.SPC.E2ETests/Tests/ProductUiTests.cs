using NUnit.Framework;
using Microsoft.Playwright.NUnit;
using MES.SPC.E2ETests.Utilities;

namespace MES.SPC.E2ETests.Tests;

[TestFixture]
public class ProductUiTests : PageTest
{
    private string _baseUrl = "";

    [SetUp]
    public void Setup()
    {
        var settings = ConfigReader.Load();
        _baseUrl = settings.WebBaseUrl;
    }

    [Test]
    public async Task Login_And_Navigate_To_ProductManagement_Should_Work()
    {
        // 1. 導航至登入頁面
        await Page.GotoAsync(_baseUrl + "/login");

        // 2. 登入
        await Page.FillAsync("input[placeholder='帳號']", "demo");
        await Page.FillAsync("input[placeholder='密碼']", "demo123");
        await Page.ClickAsync("button:has-text('登入')");

        // 3. 驗證是否導向儀表板 (Dashboard)
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/$"));
        await Expect(Page.Locator("h2")).ToContainTextAsync("Dashboard");

        // 4. 點擊「產品管理」
        await Page.ClickAsync("a:has-text('產品管理')");

        // 5. 驗證進入產品管理頁面
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/products$"));
        await Expect(Page.Locator("h2")).ToContainTextAsync("產品管理");

        // 6. 點擊「新增產品」展開表單
        await Page.ClickAsync("button:has-text('新增產品')");
        
        // 7. 驗證表單元素存在
        await Expect(Page.Locator("input[placeholder='Product Code']")).ToBeVisibleAsync();
    }
}
