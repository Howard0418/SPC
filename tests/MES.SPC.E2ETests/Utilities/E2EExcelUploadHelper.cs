using System.Text.RegularExpressions;
using Microsoft.Playwright;
using NUnit.Framework;

namespace MES.SPC.E2ETests.Utilities;

public static class E2EExcelUploadHelper
{
    /// <summary>
    /// 透過 Excel 檔案上傳：選檔 → 智慧對照 → 確認映射 → 進入預覽頁。
    /// </summary>
    public static async Task<Guid> UploadExcelToPreviewAsync(IPage page, string xlsxPath)
    {
        E2EExcelFixtures.EnsureAll();
        Assert.That(File.Exists(xlsxPath), Is.True, $"Excel 夾具不存在：{xlsxPath}");

        await page.Locator("input[type='file']").SetInputFilesAsync(xlsxPath);
        await page.Locator("h3:has-text('智慧欄位對照器')").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });
        await page.Locator("text=即時對照前 3 筆資料預覽").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });
        await E2EUiPacing.PauseAsync("Excel 對照預覽");

        await page.ClickAsync("button:has-text('確認映射並上傳批次')");
        await page.WaitForURLAsync(new Regex("/uploads/[0-9a-f-]+/preview$"), new() { Timeout = 60000 });
        await E2EUiPacing.PauseAsync("進入匯入預覽頁");

        var match = Regex.Match(page.Url, @"/uploads/([0-9a-f-]+)/preview");
        Assert.That(match.Success, Is.True, "應導向預覽頁");
        return Guid.Parse(match.Groups[1].Value);
    }
}
