using NUnit.Framework;
using Microsoft.Playwright.NUnit;
using MES.SPC.E2ETests.Utilities;
using System.Net.Http.Json;

namespace MES.SPC.E2ETests.Tests;

[TestFixture]
public class SpcChartUiTests : PageTest
{
    private string _baseUrl = "";
    private string _apiUrl = "";

    [SetUp]
    public void Setup()
    {
        var settings = ConfigReader.Load();
        _baseUrl = settings.WebBaseUrl;
        _apiUrl = settings.ApiBaseUrl;
    }

    [Test]
    public async Task SpcChart_Should_Load_Data_Correctly()
    {
        using var http = new HttpClient { BaseAddress = new Uri(_apiUrl) };

        var firstPpc = await E2ESpcDataHelper.GetUsablePpcAsync(http);
        var seedJson = await E2ESpcDataHelper.TrySeedMeasurementsAsync(http, firstPpc.Id);

        try
        {
            await Page.GotoAsync(_baseUrl + "/login");
            await Page.FillAsync("input[placeholder='帳號']", "demo");
            await Page.FillAsync("input[placeholder='密碼']", "demo123");
            await Page.ClickAsync("button:has-text('登入')");

            await Page.GotoAsync($"{_baseUrl}/spc?ppcId={firstPpc.Id}");
            await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

            var errorDiv = Page.Locator("div.bg-red-500\\/10");
            var hasError = await errorDiv.IsVisibleAsync();
            if (hasError)
            {
                var errText = await errorDiv.InnerTextAsync();
                Assert.Fail($"頁面出現錯誤訊息：{errText}");
            }

            var canvas = Page.Locator("canvas").First;
            await Expect(canvas).ToBeVisibleAsync(new() { Timeout = 10000 });
        }
        finally
        {
            await E2ESpcDataHelper.TryClearSeedMeasurementsAsync(http, seedJson?.BatchId);
        }
    }
}
