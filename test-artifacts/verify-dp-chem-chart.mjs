import { chromium } from "@playwright/test";

const url = process.argv[2] || "http://127.0.0.1:4173/spc?ppcId=530";
const screenshot = "D:/SPC/test-artifacts/dp-chem-chart-530.png";

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
  canvasCount,
  hasChemicalTab: bodyText.includes("藥液管制項目") || bodyText.includes("Chemical Control"),
  hasDpData: bodyText.includes("除鈀線") || bodyText.includes("除鈀槽") || bodyText.includes("主劑"),
  hasImr: bodyText.includes("I-MR") || bodyText.includes("I_MR"),
  quickPanelPresent: bodyText.includes("快速 ppcId") || bodyText.includes("E2E / 系統測試"),
  visibleError: (bodyText.match(/找不到[^\n]*|無法[^\n]*|失敗[^\n]*|錯誤[^\n]*/g) || []).slice(0, 5),
  consoleErrors: consoleErrors.slice(0, 5),
  screenshot
}, null, 2));
