import { chromium } from "@playwright/test";

const url = process.argv[2] || "http://127.0.0.1:4173/spc?ppcId=529";
const screenshot = "D:/SPC/test-artifacts/chem-spc-chart-529.png";

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 1440, height: 1100 } });
const consoleErrors = [];

page.on("console", (msg) => {
  if (msg.type() === "error") consoleErrors.push(msg.text());
});
page.on("pageerror", (err) => consoleErrors.push(err.message));

await page.goto(url, { waitUntil: "networkidle", timeout: 60000 });
await page.waitForTimeout(3000);

const bodyText = await page.locator("body").innerText({ timeout: 10000 });
const canvasCount = await page.locator("canvas").count();
await page.screenshot({ path: screenshot, fullPage: true });
await browser.close();

console.log(JSON.stringify({
  url: page.url(),
  quickPanelPresent: bodyText.includes("快速 ppcId") || bodyText.includes("E2E / 系統測試"),
  canvasCount,
  hasChem: bodyText.includes("藥液管制項目") || bodyText.includes("Chemical Control"),
  hasTestItem: bodyText.includes("CHEM-WEB-20260612081728") || bodyText.includes("藥水 pH"),
  hasChartType: bodyText.includes("XBAR_R") || bodyText.includes("平均數") || bodyText.includes("全距"),
  visibleError: (bodyText.match(/找不到[^\n]*|無法[^\n]*|失敗[^\n]*|錯誤[^\n]*/g) || []).slice(0, 5),
  consoleErrors: consoleErrors.slice(0, 5),
  screenshot
}, null, 2));
