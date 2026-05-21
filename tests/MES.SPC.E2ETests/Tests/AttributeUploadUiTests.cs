using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using MES.SPC.E2ETests.Utilities;
using NUnit.Framework;

namespace MES.SPC.E2ETests.Tests;

/// <summary>
/// 計數型 Excel 上傳與預覽。
/// </summary>
[TestFixture]
public class AttributeUploadUiTests : PageTest
{
    private string _baseUrl = "";
    private string _apiUrl = "";
    private E2EAttributeMasterDataContext? _attrCtx;

    [SetUp]
    public async Task Setup()
    {
        var settings = ConfigReader.Load();
        _baseUrl = settings.WebBaseUrl;
        _apiUrl = settings.ApiBaseUrl;
        _attrCtx = await E2EAttributeMasterDataHelper.EnsureAsync(_apiUrl);
        E2EExcelFixtures.WriteAttributeValidForContext(_attrCtx);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_attrCtx is not null)
            await E2EAttributeMasterDataHelper.CleanupAsync(_apiUrl, _attrCtx);
    }

    [Test]
    public async Task Attribute_Excel_Upload_Should_Show_Preview_With_Valid_Rows()
    {
        var ctx = _attrCtx ?? throw new InvalidOperationException("Attribute master data not initialized.");
        await E2EAuthHelper.LoginAsync(Page, _baseUrl);

        await Page.ClickAsync("a:has-text('計數型資料匯入')");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/uploads/attribute$"));

        var xlsx = E2EExcelFixtures.AttributeValidPathFor(ctx);
        await E2EExcelUploadHelper.UploadExcelToPreviewAsync(Page, xlsx);

        await Expect(Page.Locator("h1:has-text('匯入資料檢核與異常對照預覽')")).ToBeVisibleAsync();

        var validCountHeading = Page.Locator("text=通過校驗列數").Locator("..").Locator("h3");
        var validText = await validCountHeading.InnerTextAsync();
        Assert.That(int.Parse(validText.Replace("筆", "").Trim()), Is.GreaterThan(0));

        await Expect(Page.Locator("table tbody tr").First).ToBeVisibleAsync();
    }
}
