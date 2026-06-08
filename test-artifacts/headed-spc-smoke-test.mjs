import playwright from "../frontend/mes-spc-web/node_modules/playwright/index.js";
import fs from "node:fs/promises";
import path from "node:path";

const { chromium } = playwright;
const baseUrl = "http://172.16.110.27:8083/";
const apiUrl = "http://172.16.110.27:8082/api/v2/spc/chart-types";
const stamp = new Date().toISOString().replace(/[-:T.Z]/g, "").slice(0, 14);
const outDir = path.resolve("test-artifacts", `headed-smoke-${stamp}`);
await fs.mkdir(outDir, { recursive: true });

const browser = await chromium.launch({
  headless: false,
  slowMo: 300,
});

const page = await browser.newPage({ viewport: { width: 1440, height: 950 } });
const consoleErrors = [];
const pageErrors = [];

page.on("console", (msg) => {
  if (msg.type() === "error") consoleErrors.push(msg.text());
});
page.on("pageerror", (error) => pageErrors.push(error.message));

const result = {
  baseUrl,
  apiUrl,
  outDir,
  steps: [],
  consoleErrors,
  pageErrors,
};

try {
  const apiResponse = await page.request.get(apiUrl);
  result.steps.push({
    name: "API chart types",
    status: apiResponse.status(),
    ok: apiResponse.ok(),
  });

  await page.goto(baseUrl, { waitUntil: "domcontentloaded", timeout: 30000 });
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(outDir, "01-home.png"), fullPage: true });
  result.steps.push({
    name: "Open home",
    url: page.url(),
    title: await page.title(),
    ok: true,
    screenshot: path.join(outDir, "01-home.png"),
  });

  const spcLink = page.getByText("SPC 管制圖", { exact: true });
  if (await spcLink.count() > 0) {
    await spcLink.click();
  } else {
    await page.goto(new URL("/spc", baseUrl).toString(), { waitUntil: "domcontentloaded", timeout: 30000 });
  }
  await page.waitForTimeout(2000);
  await page.screenshot({ path: path.join(outDir, "02-spc-chart.png"), fullPage: true });
  result.steps.push({
    name: "Open SPC chart",
    url: page.url(),
    ok: true,
    screenshot: path.join(outDir, "02-spc-chart.png"),
  });

  const alertsLink = page.getByText("異常通報總覽", { exact: true });
  if (await alertsLink.count() > 0) {
    await alertsLink.click();
    await page.waitForTimeout(1500);
    await page.screenshot({ path: path.join(outDir, "03-alerts.png"), fullPage: true });
    result.steps.push({
      name: "Open alerts",
      url: page.url(),
      ok: true,
      screenshot: path.join(outDir, "03-alerts.png"),
    });
  }

  await fs.writeFile(path.join(outDir, "result.json"), JSON.stringify(result, null, 2), "utf8");
  console.log(JSON.stringify(result, null, 2));
} finally {
  await page.waitForTimeout(3000);
  await browser.close();
}
