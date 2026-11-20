import { chromium } from "playwright";

const browser = await chromium.launch({ headless: true });
try {
  const page = await browser.newPage({ ignoreHTTPSErrors: true });
  const response = await page.goto("https://spctest.pmr.com.tw/api/version", { waitUntil: "domcontentloaded" });
  if (!response || response.status() !== 200) throw new Error("Browser API version request failed");
  const payload = JSON.parse(await page.locator("body").innerText());
  if (payload.version !== "0.1.161" || payload.environment !== "test") {
    throw new Error(`Unexpected browser API payload: ${JSON.stringify(payload)}`);
  }
  console.log(JSON.stringify({ status: response.status(), url: page.url(), ...payload }));
} finally {
  await browser.close();
}
