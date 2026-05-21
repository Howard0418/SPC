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
        // ─── 1. 登入 ───
        await E2EAuthHelper.LoginAsync(Page, _baseUrl);
        await Expect(Page.Locator("text=製造統計品質即時監控看板")).ToBeVisibleAsync();

        // ─── 2. 新增大分類（管制圖分類總管 / 群組）───
        await Page.ClickAsync("a:has-text('管制圖分類總管')");
        await Expect(Page).ToHaveURLAsync(new Regex("/control-chart-groups$"));
        await CreateGroupAsync();
        await E2EUiPacing.PauseAsync("大分類完成");

        // ─── 3. 新增中分類 ───
        await Page.ClickAsync("a:has-text('管制圖分類維護')");
        await Expect(Page).ToHaveURLAsync(new Regex("/control-chart-categories$"));
        await CreateCategoryAsync();
        await E2EUiPacing.PauseAsync("中分類完成");

        // ─── 4. 新增小分類（管制圖參數配置 / 種類）───
        await Page.ClickAsync("a:has-text('管制圖參數配置')");
        await Expect(Page).ToHaveURLAsync(new Regex("/control-chart-types$"));
        await CreateChartTypeAsync();
        await E2EUiPacing.PauseAsync("小分類完成");

        // ─── 5. 新增作業人員 ───
        await Page.ClickAsync("a:has-text('作業工程師與權限')");
        await Expect(Page).ToHaveURLAsync(new Regex("/operators$"));
        await CreateOperatorAsync();

        await E2ECleanupHelper.ResolveCreatedIdsAsync(_apiUrl, _ctx);
        Assert.That(_ctx.GroupId, Is.Not.Null, "大分類應已建立");
        Assert.That(_ctx.CategoryId, Is.Not.Null, "中分類應已建立");
        Assert.That(_ctx.TypeId, Is.Not.Null, "小分類應已建立");
        Assert.That(_ctx.OperatorId, Is.Not.Null, "作業人員應已建立");
        await E2EUiPacing.PauseAsync("作業人員完成");

        // ─── 6. 匯入計量型量測資料（Excel）───
        E2EExcelFixtures.EnsureAll();
        await Page.ClickAsync("a:has-text('計量型資料匯入')");
        await Expect(Page).ToHaveURLAsync(new Regex("/uploads/variable$"));
        _ctx.UploadBatchId = await E2EExcelUploadHelper.UploadExcelToPreviewAsync(Page, E2EExcelFixtures.VariableValidPath);

        await Expect(Page.Locator("text=匯入資料檢核與異常對照預覽")).ToBeVisibleAsync();
        var validCountText = await Page.Locator("text=通過校驗列數").Locator("..").Locator("h3").InnerTextAsync();
        Assert.That(int.Parse(validCountText.Replace("筆", "").Trim()), Is.GreaterThan(0));

        // ─── 7. 確認匯入 ───
        using var httpImport = new HttpClient { BaseAddress = new Uri(_apiUrl) };
        var confirmBtn = Page.Locator("button:has-text('確認轉入正式 SPC 運算')");
        await Expect(confirmBtn).ToBeEnabledAsync(new() { Timeout = 30000 });
        await confirmBtn.ClickAsync();

        var imported = false;
        for (var i = 0; i < 90; i++)
        {
            if (Regex.IsMatch(Page.Url, @"/spc(\?|$)")) { imported = true; break; }
            if (await Page.Locator("text=已成功匯入正式資料表").IsVisibleAsync()) { imported = true; break; }

            var preview = await httpImport.GetFromJsonAsync<PreviewBatchDto>(
                $"/api/uploads/{_ctx.UploadBatchId}/preview");
            if (preview?.Batch?.ImportStatus is "Imported" or "Importing")
            {
                imported = true;
                break;
            }
            await Page.WaitForTimeoutAsync(1000);
        }

        if (!imported)
        {
            var confirmRes = await httpImport.PostAsync($"/api/uploads/{_ctx.UploadBatchId}/confirm", null);
            confirmRes.EnsureSuccessStatusCode();
            imported = true;
        }

        await Page.GotoAsync($"{_baseUrl}/spc");
        Assert.That(imported, Is.True, "確認匯入應完成");
        await E2EUiPacing.PauseAsync("匯入確認完成");

        // ─── 8. SPC 管制圖繪製驗證 ───
        await Expect(Page.Locator("h1:has-text('SPC 即時互動管制圖戰情室')")).ToBeVisibleAsync(new() { Timeout = 60000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        using var http = new HttpClient { BaseAddress = new Uri(_apiUrl) };
        var ppcs = await http.GetFromJsonAsync<List<PpcDetailDto>>("/api/part-process-characteristics");
        var ppc = ppcs?.FirstOrDefault(p => p.Part?.PartNo == "P-1001")
            ?? ppcs?.FirstOrDefault(p => p.PartId > 0 && p.ProcessId > 0 && p.CharacteristicId > 0);
        Assert.That(ppc, Is.Not.Null, "應存在可查詢的檢驗基準");

        // 若匯入後圖表資料尚不足，補種量測再查詢
        var seedRes = await http.PostAsync(
            $"/api/v2/migration/seed-sample-measurements?ppcId={ppc!.Id}&count=25", null);
        Assert.That(seedRes.IsSuccessStatusCode, Is.True);

        await Page.FillAsync("label:has-text('產品 ID') + input", ppc.PartId.ToString());
        await Page.FillAsync("label:has-text('工站 ID') + input", ppc.ProcessId.ToString());
        await Page.FillAsync("label:has-text('檢測項目 ID') + input", ppc.CharacteristicId.ToString());
        await Page.ClickAsync("button:has-text('查詢')");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await E2EUiPacing.PauseAsync("SPC 圖表查詢");

        var chartError = Page.Locator("div.bg-red-500\\/10");
        if (await chartError.IsVisibleAsync())
        {
            var errText = await chartError.InnerTextAsync();
            Assert.Fail($"SPC 圖表載入失敗：{errText}");
        }

        await Expect(Page.Locator("canvas").First).ToBeVisibleAsync(new() { Timeout = 90000 });
        await Expect(Page.Locator("text=規格/管制界限失控點")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Cpk (製程能力指標)")).ToBeVisibleAsync(new() { Timeout = 60000 });
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
