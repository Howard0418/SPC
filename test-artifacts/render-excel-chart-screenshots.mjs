import playwright from "../frontend/mes-spc-web/node_modules/playwright/index.js";
import fs from "node:fs/promises";
import path from "node:path";

const { chromium } = playwright;
const resultPath = process.argv[2];
if (!resultPath) throw new Error("Usage: node test-artifacts/render-excel-chart-screenshots.mjs <result.json>");

const result = JSON.parse(await fs.readFile(resultPath, "utf8"));
const outDir = result.outDir;
const echartsPath = path.resolve("frontend/mes-spc-web/node_modules/echarts/dist/echarts.min.js");

const browser = await chromium.launch({ headless: false, slowMo: 150 });
const page = await browser.newPage({ viewport: { width: 1440, height: 950 } });
page.setDefaultTimeout(90_000);

async function fetchJson(url) {
  const response = await page.request.get(url, { timeout: 90_000 });
  const text = await response.text();
  return JSON.parse(text);
}

function pointValue(p) {
  if (p.value != null) return p.value;
  if (p.xbar != null) return p.xbar;
  return null;
}

function secondaryValue(p, chartType) {
  if (chartType === "XBAR_R") return p.range ?? null;
  if (chartType === "XBAR_S") return p.stdDev ?? null;
  if (chartType === "I-MR" || chartType === "I_MR") return p.movingRange ?? null;
  return null;
}

for (let index = 0; index < result.charts.length; index += 1) {
  const item = result.charts[index];
  const chart = await fetchJson(item.query.apiUrl);
  const points = chart.chartData?.points || [];
  const secondaryPoints = chart.secondaryChartData?.points || [];
  const labels = points.map((p, i) => p.lotNo || `#${i + 1}`);
  const values = points.map(pointValue);
  const secondaryValues = secondaryPoints.length > 0
    ? secondaryPoints.map((p) => p.value ?? secondaryValue(p, chart.chartType))
    : points.map((p) => secondaryValue(p, chart.chartType));

  const html = `<!doctype html>
<html>
<head>
  <meta charset="utf-8" />
  <title>${item.chartCode} Excel Import Chart</title>
  <style>
    body { margin: 0; font-family: Arial, "Microsoft JhengHei", sans-serif; background: #f8fafc; color: #0f172a; }
    .wrap { padding: 28px; }
    .title { font-size: 26px; font-weight: 800; margin-bottom: 6px; }
    .meta { font-size: 13px; color: #475569; line-height: 1.7; margin-bottom: 16px; }
    #chart { width: 1360px; height: 650px; background: white; border: 1px solid #e2e8f0; border-radius: 12px; }
    .query { margin-top: 14px; padding: 12px 14px; background: #eef2ff; border: 1px solid #c7d2fe; border-radius: 10px; font-size: 13px; line-height: 1.7; }
    code { font-family: Consolas, monospace; color: #3730a3; }
  </style>
</head>
<body>
  <div class="wrap">
    <div class="title">${item.chartCode} 管制圖 - Excel 匯入產圖驗證</div>
    <div class="meta">
      Chart API: ${item.query.apiUrl}<br />
      查詢條件：產品 ${item.master.partNo}，製程 ${item.master.processCode}，檢驗項目 ${item.master.characteristicCode}<br />
      匯入批次：${item.upload.batchId}，點數：${item.chart.pointCount}，OOC：${item.chart.outOfControlCount}
    </div>
    <div id="chart"></div>
    <div class="query">畫面查詢方式：到 <strong>SPC 查詢 / 管制圖</strong>，依上方產品、製程、檢驗項目搜尋並選取。若要用 API 驗證，開啟：<code>${item.query.apiUrl}</code></div>
  </div>
</body>
</html>`;

  await page.setContent(html, { waitUntil: "load" });
  await page.addScriptTag({ path: echartsPath });
  await page.evaluate(({ chartType, labels, values, secondaryValues, limits, points }) => {
    const chartEl = document.getElementById("chart");
    const instance = echarts.init(chartEl);
    const isDual = chartType === "XBAR_R" || chartType === "XBAR_S" || chartType === "I-MR" || chartType === "I_MR";
    const markLine = {
      symbol: "none",
      data: [
        limits?.ucl != null ? { yAxis: limits.ucl, name: "UCL", lineStyle: { color: "#dc2626", type: "dashed" } } : null,
        limits?.cl != null ? { yAxis: limits.cl, name: "CL", lineStyle: { color: "#2563eb", type: "dashed" } } : null,
        limits?.lcl != null ? { yAxis: limits.lcl, name: "LCL", lineStyle: { color: "#dc2626", type: "dashed" } } : null,
      ].filter(Boolean),
    };
    const topData = values.map((value, i) => ({
      value,
      itemStyle: { color: points[i]?.outOfControl ? "#dc2626" : "#2563eb" },
      symbolSize: points[i]?.outOfControl ? 12 : 8,
    }));
    const option = {
      animation: false,
      tooltip: { trigger: "axis" },
      legend: { top: 8 },
      grid: isDual
        ? [{ left: 70, right: 30, top: 60, height: 250 }, { left: 70, right: 30, top: 390, height: 190 }]
        : [{ left: 70, right: 30, top: 60, bottom: 90 }],
      xAxis: isDual
        ? [{ type: "category", data: labels, gridIndex: 0, axisLabel: { rotate: 30 } }, { type: "category", data: labels, gridIndex: 1, axisLabel: { rotate: 30 } }]
        : [{ type: "category", data: labels, axisLabel: { rotate: 30 } }],
      yAxis: isDual
        ? [{ type: "value", gridIndex: 0, scale: true }, { type: "value", gridIndex: 1, scale: true }]
        : [{ type: "value", scale: true }],
      series: isDual
        ? [
            { name: chartType.includes("XBAR") ? "Xbar" : "Individual", type: "line", data: topData, markLine, xAxisIndex: 0, yAxisIndex: 0 },
            { name: chartType === "XBAR_R" ? "Range" : chartType === "XBAR_S" ? "Std Dev" : "Moving Range", type: "line", data: secondaryValues, xAxisIndex: 1, yAxisIndex: 1 },
          ]
        : [{ name: chartType, type: "line", data: topData, markLine }],
    };
    instance.setOption(option);
  }, {
    chartType: chart.chartType,
    labels,
    values,
    secondaryValues,
    limits: chart.limits,
    points,
  });

  await page.waitForTimeout(500);
  const renderPath = path.join(outDir, `${String(index + 1).padStart(2, "0")}-${item.chartCode}-rendered-chart.png`);
  await page.screenshot({ path: renderPath, fullPage: true });
  item.renderedChartScreenshot = renderPath;
}

await fs.writeFile(resultPath, JSON.stringify(result, null, 2), "utf8");
console.log(JSON.stringify({
  resultPath,
  screenshots: result.charts.map((x) => ({ chartCode: x.chartCode, screenshot: x.renderedChartScreenshot, apiUrl: x.query.apiUrl, instruction: x.query.uiInstruction })),
}, null, 2));

await browser.close();
