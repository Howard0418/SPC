using Microsoft.Playwright;

namespace MES.SPC.E2ETests.Utilities;

public static class E2EAuthHelper
{
    public static async Task LoginAsync(IPage page, string baseUrl, string username = "demo", string password = "demo123")
    {
        await page.GotoAsync(baseUrl + "/login");
        await page.FillAsync("input[placeholder='帳號']", username);
        await page.FillAsync("input[placeholder='密碼']", password);
        await page.Locator("button:has-text('登入')").ClickAsync(new LocatorClickOptions { Timeout = 10000 });
        // Vue Router 為 SPA，以 Dashboard 標題等待登入完成
        await page.WaitForSelectorAsync("text=製造統計品質即時監控看板", new() { Timeout = 15000 });
        await E2EUiPacing.PauseAsync("登入完成");
    }
}
