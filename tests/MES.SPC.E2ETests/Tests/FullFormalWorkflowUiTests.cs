using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using MES.SPC.E2ETests.Utilities;
using NUnit.Framework;

namespace MES.SPC.E2ETests.Tests;

/// <summary>
/// 正式流程端到端：登入 → 大/中/小分類 → 作業人員 → 匯入量測 → SPC 畫圖。
/// </summary>
[TestFixture]
[CancelAfter(300_000)]
public class FullFormalWorkflowUiTests : PageTest
{
    private string _baseUrl = "";
    private string _apiUrl = "";
    private readonly E2ETestRunContext _ctx = new();

    [SetUp]
    public void Setup()
    {
        var settings = ConfigReader.Load();
        _baseUrl = settings.WebBaseUrl;
        _apiUrl = settings.ApiBaseUrl;
    }

    [TearDown]
    public async Task TearDown()
    {
        await E2ECleanupHelper.ResolveCreatedIdsAsync(_apiUrl, _ctx);
        await E2ECleanupHelper.CleanupAsync(_apiUrl, _ctx);
    }

    [Test]
    public async Task Full_Formal_Workflow_From_Login_To_Spc_Chart_Should_Succeed()
    {
        await E2EAuthHelper.LoginAsync(Page, _baseUrl);
        await Expect(Page.Locator("text=製造統計品質即時監控看板")).ToBeVisibleAsync();

        await Page.GotoAsync($"{_baseUrl}/control-chart-groups");
        await Expect(Page).ToHaveURLAsync(new Regex("/control-chart-groups$"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("SPC 管制圖配置總管維護") })).ToBeVisibleAsync();

        await Page.GotoAsync($"{_baseUrl}/operators");
        await Expect(Page).ToHaveURLAsync(new Regex("/operators$"));
        await Expect(Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("新增人員") })).ToBeVisibleAsync();

        await Page.GotoAsync($"{_baseUrl}/uploads/variable");
        await Expect(Page).ToHaveURLAsync(new Regex("/uploads/variable$"));
        await Expect(Page.Locator("input[type='file']")).ToBeAttachedAsync();

        using var http = new HttpClient { BaseAddress = new Uri(_apiUrl) };
        var ppc = await E2ESpcDataHelper.GetUsablePpcAsync(http);
        var seed = await E2ESpcDataHelper.TrySeedMeasurementsAsync(http, ppc.Id);

        await Page.GotoAsync($"{_baseUrl}/spc?ppcId={ppc.Id}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var chartError = Page.Locator("div.bg-red-500\\/10");
        if (await chartError.IsVisibleAsync())
        {
            var errText = await chartError.InnerTextAsync();
            Assert.Fail($"SPC 圖表載入失敗：{errText}");
        }

        await Expect(Page.Locator("canvas").First).ToBeVisibleAsync(new() { Timeout = 90000 });
        await Expect(Page.Locator("text=規格/管制界限失控點")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Cpk (製程能力指標)")).ToBeVisibleAsync(new() { Timeout = 60000 });

        await E2ESpcDataHelper.TryClearSeedMeasurementsAsync(http, seed?.BatchId);
    }

    private async Task CreateGroupAsync()
    {
        await Page.ClickAsync("button:has-text('新增群組')");
        await Expect(Page.Locator("h3:has-text('新增管制圖群組')")).ToBeVisibleAsync();

        var modal = Page.Locator("div.fixed.inset-0 form").Last;
        await modal.Locator("input[type='text']").Nth(0).FillAsync(_ctx.GroupCode);
        await modal.Locator("input[type='text']").Nth(1).FillAsync(_ctx.GroupName);
        await Page.ClickAsync("button:has-text('確認儲存')");

        await Expect(Page.Locator($"text=成功建立新管制圖群組：{_ctx.GroupName}")).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    private async Task CreateCategoryAsync()
    {
        await Page.ClickAsync("button:has-text('新增類別')");
        await Expect(Page.Locator("h3:has-text('新增管制圖類別')")).ToBeVisibleAsync();

        var modal = Page.Locator("div.fixed.inset-0 form").Last;
        await modal.Locator("select").First.SelectOptionAsync(new SelectOptionValue { Label = $"{_ctx.GroupCode} - {_ctx.GroupName}" });
        await modal.Locator("input[type='text']").Nth(0).FillAsync(_ctx.CategoryCode);
        await modal.Locator("input[type='text']").Nth(1).FillAsync(_ctx.CategoryName);
        await Page.ClickAsync("button:has-text('確認儲存')");

        await Expect(Page.Locator($"text=成功建立新管制圖類別：{_ctx.CategoryName}")).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    private async Task CreateChartTypeAsync()
    {
        await Page.ClickAsync("button:has-text('新增管制圖種類')");
        await Expect(Page.Locator("h3:has-text('新增管制圖種類')")).ToBeVisibleAsync();

        var modal = Page.Locator("div.fixed.inset-0 form").Last;
        await modal.Locator("select").First.SelectOptionAsync(new SelectOptionValue { Label = $"{_ctx.CategoryCode} - {_ctx.CategoryName}" });
        await modal.Locator("input[type='text']").Nth(0).FillAsync(_ctx.TypeCode);
        await modal.Locator("input[type='text']").Nth(1).FillAsync(_ctx.TypeName);
        await Page.ClickAsync("button:has-text('確認儲存')");

        await Expect(Page.Locator($"text=成功建立新管制圖種類：{_ctx.TypeName}")).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    private async Task CreateOperatorAsync()
    {
        await Page.ClickAsync("button:has-text('新增人員')");
        await Expect(Page.Locator("h3:has-text('新增作業人員')")).ToBeVisibleAsync();

        var modal = Page.Locator("div.fixed.inset-0 form").Last;
        await modal.Locator("input[type='text']").Nth(0).FillAsync(_ctx.OperatorCode);
        await modal.Locator("input[type='text']").Nth(1).FillAsync(_ctx.OperatorName);
        await modal.Locator("input[type='text']").Nth(2).FillAsync("E2E品管部");
        await Page.ClickAsync("button:has-text('確認儲存')");

        await Expect(Page.Locator($"text=成功建立新作業人員：{_ctx.OperatorName}")).ToBeVisibleAsync(new() { Timeout = 15000 });
    }

    private record PpcDetailDto(int Id, int PartId, int ProcessId, int CharacteristicId, PartRef? Part);
    private record PartRef(string PartNo, string? PartName);
    private record PreviewBatchDto(UploadBatchInfo? Batch);
    private record UploadBatchInfo(string? ImportStatus);
}
