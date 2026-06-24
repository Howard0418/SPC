using System.Net.Http.Json;
using System.Text.RegularExpressions;
using MES.SPC.E2ETests.Utilities;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MES.SPC.E2ETests.Tests;

[TestFixture]
public class VariableExcelImportToSpcFlowUiTests : PageTest
{
    private string _baseUrl = "";
    private string _apiUrl = "";
    private E2EVariableMasterDataContext? _ctx;
    private Guid? _uploadBatchId;

    [SetUp]
    public async Task Setup()
    {
        var settings = ConfigReader.Load();
        _baseUrl = settings.WebBaseUrl;
        _apiUrl = settings.ApiBaseUrl;

        _ctx = await E2EVariableMasterDataHelper.EnsureAsync(_apiUrl);
        E2EExcelFixtures.WriteVariableValidForContext(_ctx);
    }

    [TearDown]
    public async Task TearDown()
    {
        using var http = new HttpClient { BaseAddress = new Uri(_apiUrl) };
        if (_uploadBatchId.HasValue)
        {
            try { await http.DeleteAsync($"/api/uploads/{_uploadBatchId.Value}"); } catch { }
        }

        if (_ctx is not null)
            await E2EVariableMasterDataHelper.CleanupAsync(_apiUrl, _ctx);
    }

    [Test]
    public async Task Variable_Excel_Upload_Preview_Confirm_Should_Open_Spc_Chart()
    {
        var ctx = _ctx ?? throw new InvalidOperationException("Variable master data not initialized.");
        var xlsx = E2EExcelFixtures.VariableValidPathFor(ctx);

        await E2EAuthHelper.LoginAsync(Page, _baseUrl);

        await Page.GotoAsync($"{_baseUrl}/uploads/variable");
        await Expect(Page).ToHaveURLAsync(new Regex("/uploads/variable$"));

        _uploadBatchId = await E2EExcelUploadHelper.UploadExcelToPreviewAsync(Page, xlsx);

        await Expect(Page.Locator("h1:has-text('匯入資料檢核與異常對照預覽')")).ToBeVisibleAsync();

        var validCountText = await Page.Locator("text=通過校驗列數").Locator("..").Locator("h3").InnerTextAsync();
        Assert.That(int.Parse(validCountText.Replace("筆", "").Trim()), Is.GreaterThan(0));

        await Expect(Page.Locator("table tbody tr").First).ToBeVisibleAsync();

        var confirmButton = Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("確認轉入正式 SPC 運算") });
        await Expect(confirmButton).ToBeEnabledAsync(new() { Timeout = 30000 });
        await confirmButton.ClickAsync();

        await Page.WaitForURLAsync(new Regex($"/spc\\?"), new() { Timeout = 60000 });
        await Expect(Page).ToHaveURLAsync(new Regex($"ppcId={ctx.PpcId}"));
        await Expect(Page).ToHaveURLAsync(new Regex($"uploadBatchId={_uploadBatchId}"));

        using var http = new HttpClient { BaseAddress = new Uri(_apiUrl) };
        PreviewResponse? preview = null;
        for (var i = 0; i < 30; i++)
        {
            preview = await http.GetFromJsonAsync<PreviewResponse>($"/api/uploads/{_uploadBatchId}/preview");
            if (preview?.Batch?.ImportStatus is "Imported" || preview?.Batch?.IsConfirmed == true) break;
            await Page.WaitForTimeoutAsync(1000);
        }

        Assert.That(preview?.Batch?.ImportStatus is "Imported" || preview?.Batch?.IsConfirmed == true,
            "Excel 批次應完成確認匯入。");

        using var chartRes = await http.GetAsync($"/api/v1/spc/chart?ppcId={ctx.PpcId}&uploadBatchId={_uploadBatchId}");
        chartRes.EnsureSuccessStatusCode();

        var chartError = Page.Locator("div.bg-red-500\\/10");
        if (await chartError.IsVisibleAsync())
        {
            Assert.Fail("SPC 圖表載入失敗：" + await chartError.InnerTextAsync());
        }

        await Expect(Page.Locator("canvas").First).ToBeVisibleAsync(new() { Timeout = 90000 });
        await Expect(Page.Locator("text=規格/管制界限失控點")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Cpk (製程能力指標)")).ToBeVisibleAsync();
    }

    private sealed record PreviewResponse(PreviewBatch? Batch);
    private sealed record PreviewBatch(string? ImportStatus, bool IsConfirmed);
}
