using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using MES.SPC.E2ETests.Utilities;
using NUnit.Framework;

namespace MES.SPC.E2ETests.Tests;

/// <summary>
/// 計量型 Excel 上傳與預覽錯誤列標記。
/// </summary>
[TestFixture]
public class UploadPreviewUiTests : PageTest
{
    private string _baseUrl = "";

    [SetUp]
    public void Setup()
    {
        _baseUrl = ConfigReader.Load().WebBaseUrl;
        E2EExcelFixtures.EnsureAll();
    }

    [Test]
    public async Task Variable_Excel_Upload_With_Errors_Should_Show_Preview_With_Red_Rows()
    {
        await E2EAuthHelper.LoginAsync(Page, _baseUrl);

        await Page.ClickAsync("a:has-text('計量型資料匯入')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/uploads/variable$"));

        await E2EExcelUploadHelper.UploadExcelToPreviewAsync(Page, E2EExcelFixtures.VariableMixedPath);

        await Expect(Page.Locator("h1:has-text('匯入資料檢核與異常對照預覽')")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=上傳資料明細與檢核結果預覽")).ToBeVisibleAsync();

        var errorCountHeading = Page.Locator("text=攔截錯誤列數").Locator("..").Locator("h3");
        var errorText = await errorCountHeading.InnerTextAsync();
        Assert.That(int.Parse(errorText.Replace("筆", "").Trim()), Is.GreaterThan(0));

        await Expect(Page.Locator("tbody tr.border-l-4.border-red-500").First).ToBeVisibleAsync();
        await Expect(Page.Locator("table tbody tr").First).ToBeVisibleAsync();
    }
}
