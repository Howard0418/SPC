using NUnit.Framework;
using Microsoft.Playwright;
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

        // 4. 點擊「產品料號主檔」
        await Page.GetByRole(AriaRole.Link, new() { NameRegex = new System.Text.RegularExpressions.Regex("產品料號主檔") }).First.ClickAsync();

        // 5. 驗證進入產品管理頁面
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/parts"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { NameRegex = new System.Text.RegularExpressions.Regex("產品料號主檔維護") })).ToBeVisibleAsync();

        // 6. 點擊「新增料號」展開表單
        await Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex("新增料號") }).ClickAsync();
        
        // 7. 驗證表單元素存在
        await Expect(Page.GetByPlaceholder("例如：P-1001")).ToBeVisibleAsync();
    }
}
