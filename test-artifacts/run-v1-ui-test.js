const { chromium } = require("playwright");

(async () => {
  const browser = await chromium.launch({ headless: false, slowMo: 150 });
  const page = await browser.newPage({ viewport: { width: 1440, height: 950 } });
  const shots = "D:/SPC/test-artifacts";

  await page.goto("http://172.16.110.27:8083/", { waitUntil: "networkidle" });
  await page.screenshot({ path: `${shots}/v1-dashboard-published.png`, fullPage: true });
  const dashText = await page.locator("body").innerText();

  const chartBtn = page.getByRole("button", { name: /看圖/ }).first();
  let chartUrl = "";
  let canvasCount = 0;
  if (await chartBtn.count()) {
    await chartBtn.click();
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(1000);
    chartUrl = page.url();
    canvasCount = await page.locator("canvas").count();
    await page.screenshot({ path: `${shots}/v1-dashboard-to-chart.png`, fullPage: true });
  }

  await page.goto("http://172.16.110.27:8083/alerts", { waitUntil: "networkidle" });
  await page.screenshot({ path: `${shots}/v1-alerts-published.png`, fullPage: true });
  const alertsText = await page.locator("body").innerText();

  const link = page.locator("a[href^='/spc?ppcId=']").first();
  let alertsChartUrl = "";
  let alertsCanvasCount = 0;
  if (await link.count()) {
    await link.click();
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(1000);
    alertsChartUrl = page.url();
    alertsCanvasCount = await page.locator("canvas").count();
    await page.screenshot({ path: `${shots}/v1-alerts-to-chart.png`, fullPage: true });
  }

  await page.goto("http://172.16.110.27:8083/alerts-workflow", { waitUntil: "networkidle" });
  await page.screenshot({ path: `${shots}/v1-alerts-workflow.png`, fullPage: true });
  const workflowText = await page.locator("body").innerText();

  await page.goto("http://172.16.110.27:8083/v2/alerts-workflow", { waitUntil: "networkidle" });
  await page.waitForTimeout(500);
  const redirectedWorkflowUrl = page.url();

  console.log(JSON.stringify({
    dashboardHasV1Fields: /E2E|PART|PROC|CHAR|產品料號|製程/.test(dashText),
    chartUrl,
    canvasCount,
    alertsHasV1Fields: /E2E|PART|PROC|CHAR|產品料號|製程/.test(alertsText),
    alertsChartUrl,
    alertsCanvasCount,
    workflowHasV1Fields: /E2E|PART|PROC|CHAR|查看 SPC 圖表|產品料號|製程/.test(workflowText),
    redirectedWorkflowUrl
  }, null, 2));

  await browser.close();
})().catch((error) => {
  console.error(error);
  process.exit(1);
});
