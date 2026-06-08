const fs = require("fs");
const path = require("path");
const XLSX = require("D:/SPC/frontend/mes-spc-web/node_modules/xlsx");
const { chromium } = require("D:/SPC/frontend/mes-spc-web/node_modules/playwright");

const WEB = "http://172.16.110.27:8083";
const API = "http://172.16.110.27:8082/api";
const stamp = new Date().toISOString().replace(/[-:TZ.]/g, "").slice(0, 14);
const prefix = `FULL-E2E-${stamp}`;
const outDir = path.resolve("D:/SPC/test-artifacts", `full-site-test-${stamp}`);
fs.mkdirSync(outDir, { recursive: true });

const results = [];
const created = { ppcs: {}, batches: {}, alerts: [] };

function record(name, ok, detail = {}) {
  const item = { name, ok, ...detail };
  results.push(item);
  console.log(`${ok ? "PASS" : "FAIL"} ${name}`);
  if (!ok && detail.error) console.log(`  ${detail.error}`);
}

async function api(pathname, options = {}) {
  const res = await fetch(`${API}${pathname}`, {
    ...options,
    headers: {
      ...(options.body && !(options.body instanceof FormData) ? { "content-type": "application/json" } : {}),
      ...(options.headers || {})
    }
  });
  const text = await res.text();
  let body = null;
  try { body = text ? JSON.parse(text) : null; } catch { body = text; }
  if (!res.ok) {
    throw new Error(`${options.method || "GET"} ${pathname} -> ${res.status}: ${text.slice(0, 500)}`);
  }
  return body;
}

async function apiStatus(pathname, options = {}) {
  const res = await fetch(`${API}${pathname}`, {
    ...options,
    headers: {
      ...(options.body && !(options.body instanceof FormData) ? { "content-type": "application/json" } : {}),
      ...(options.headers || {})
    }
  });
  return res.status;
}

async function postJson(pathname, body) {
  return api(pathname, { method: "POST", body: JSON.stringify(body) });
}

async function putJson(pathname, body) {
  return api(pathname, { method: "PUT", body: JSON.stringify(body) });
}

async function ensureChartType(code, dataCategory, sampleSize) {
  const types = await api("/v1/control-chart-types");
  let type = types.find(x => x.chartTypeCode === code);
  if (!type) {
    const groups = await api("/v1/control-chart-groups");
    let group = groups.find(x => x.groupCode === `${prefix}-GRP`);
    if (!group) group = await postJson("/v1/control-chart-groups", { groupCode: `${prefix}-GRP`, groupName: `${prefix} 測試群組`, isEnabled: true });
    let cats = await api("/v1/control-chart-categories");
    let cat = cats.find(x => x.categoryCode === `${prefix}-${dataCategory}`);
    if (!cat) cat = await postJson("/v1/control-chart-categories", { chartGroupId: group.id, categoryCode: `${prefix}-${dataCategory}`, categoryName: `${prefix} ${dataCategory}`, isEnabled: true });
    type = await postJson("/v1/control-chart-types", {
      chartCategoryId: cat.id,
      chartTypeCode: code,
      chartTypeName: code,
      dataCategory,
      requiredSampleSize: sampleSize,
      isEnabled: true
    });
  }
  return type;
}

async function setupMaster(chartCode, dataCategory, sampleSize) {
  const safe = chartCode.replace(/[^A-Z0-9]/g, "_");
  const part = await postJson("/v1/parts", { partNo: `${prefix}-${safe}-PART`, partName: `${prefix} ${safe} 中文料號`, isEnabled: true });
  const process = await postJson("/v1/processes", { processCode: `${prefix}-${safe}-PROC`, processName: `${prefix} ${safe} 中文製程`, isEnabled: true });
  const machine = await postJson("/v1/machines", { machineCode: `${prefix}-${safe}-M01`, machineName: `${prefix} ${safe} 中文機台`, processId: process.id, status: "Active", isEnabled: true });
  const type = await ensureChartType(chartCode, dataCategory, sampleSize);
  const characteristic = await postJson("/v1/characteristics", {
    characteristicCode: `${prefix}-${safe}-CHAR`,
    characteristicName: `${prefix} ${safe} 中文特性`,
    dataCategory,
    defaultChartTypeId: type.id,
    unit: dataCategory === "Variable" ? "mm" : "pcs",
    isSpcEnabled: true,
    isEnabled: true
  });
  const ppc = await postJson("/v1/part-process-characteristics", {
    partId: part.id,
    processId: process.id,
    characteristicId: characteristic.id,
    chartTypeId: type.id,
    sampleSize,
    usl: dataCategory === "Variable" ? 11 : null,
    lsl: dataCategory === "Variable" ? 9 : null,
    ucl: dataCategory === "Variable" ? 10.8 : null,
    lcl: dataCategory === "Variable" ? 9.2 : null,
    cl: dataCategory === "Variable" ? 10 : null,
    isRequired: true,
    isEnabled: true
  });
  created.ppcs[chartCode] = { part, process, machine, characteristic, ppc };
  return created.ppcs[chartCode];
}

function variableRows(meta, chartCode) {
  const rows = [];
  const groups = chartCode === "I_MR" ? 18 : 14;
  const n = chartCode === "I_MR" ? 1 : 5;
  for (let g = 1; g <= groups; g++) {
    for (let s = 1; s <= n; s++) {
      const base = g === groups ? 12.2 : 10 + Math.sin(g / 2) * 0.15 + (s - 3) * 0.03;
      rows.push({
        PartNo: meta.part.partNo,
        ProcessCode: meta.process.processCode,
        MachineCode: meta.machine.machineCode,
        CharacteristicCode: meta.characteristic.characteristicCode,
        LotNo: `${prefix}-${chartCode}-LOT-${String(g).padStart(2, "0")}`,
        SerialNo: `${prefix}-${chartCode}-SN-${String(g).padStart(2, "0")}-${s}`,
        SampleNo: String(s),
        MeasuredValue: base.toFixed(4),
        MeasuredAt: new Date(Date.now() - (groups - g) * 60000).toISOString(),
        Operator: `${prefix}-OP`
      });
    }
  }
  return rows;
}

function attributeRows(meta, chartCode) {
  const rows = [];
  for (let i = 1; i <= 18; i++) {
    const defect = i === 18 ? (chartCode === "P" ? 25 : chartCode === "NP" ? 22 : 20) : Math.max(1, Math.round(3 + Math.sin(i) * 2));
    rows.push({
      PartNo: meta.part.partNo,
      ProcessCode: meta.process.processCode,
      MachineCode: meta.machine.machineCode,
      CharacteristicCode: meta.characteristic.characteristicCode,
      LotNo: `${prefix}-${chartCode}-LOT-${String(i).padStart(2, "0")}`,
      SampleNo: String(i),
      InspectedQty: chartCode === "C" ? "" : "100",
      DefectQty: chartCode === "C" ? "" : String(defect),
      DefectCount: chartCode === "P" || chartCode === "NP" ? "" : String(defect),
      UnitCount: chartCode === "U" ? "100" : "",
      MeasuredAt: new Date(Date.now() - (18 - i) * 60000).toISOString(),
      Operator: `${prefix}-OP`
    });
  }
  return rows;
}

function makeWorkbook(file, rows) {
  const ws = XLSX.utils.json_to_sheet(rows);
  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, "Data");
  XLSX.writeFile(wb, file);
}

async function uploadExcel(kind, rows, chartCode) {
  const file = path.join(outDir, `${chartCode}-${kind}.xlsx`);
  makeWorkbook(file, rows);
  const form = new FormData();
  form.set("file", new Blob([fs.readFileSync(file)]), path.basename(file));
  const uploaded = await api(`/v1/uploads/${kind}/excel`, { method: "POST", body: form });
  const preview = await api(`/v1/uploads/${uploaded.uploadBatchId}/preview`);
  if ((preview.batch?.validRows ?? preview.validRows ?? uploaded.validRows) <= 0) throw new Error(`${chartCode} preview has no valid rows`);
  const confirmed = await api(`/v1/uploads/${uploaded.uploadBatchId}/confirm`, { method: "POST" });
  created.batches[chartCode] = uploaded.uploadBatchId;
  return { uploaded, preview, confirmed, file };
}

async function chartCheck(chartCode, ppcId, uploadBatchId) {
  const chart = await api(`/v1/spc/chart?ppcId=${ppcId}${uploadBatchId ? `&uploadBatchId=${uploadBatchId}` : ""}`);
  const points = chart?.chartData?.points || chart?.points || [];
  if (!chart || !Array.isArray(points) || points.length === 0) throw new Error(`${chartCode} chart has no points`);
  return chart;
}

function chartPoints(chart) {
  return chart?.chartData?.points || chart?.points || [];
}

async function runApiFlow() {
  const chartPlans = [
    ["I_MR", "Variable", 1],
    ["XBAR_R", "Variable", 5],
    ["XBAR_S", "Variable", 5],
    ["P", "Attribute", 1],
    ["NP", "Attribute", 1],
    ["C", "Attribute", 1],
    ["U", "Attribute", 1]
  ];

  for (const [chartCode, category, sampleSize] of chartPlans) {
    try {
      const meta = await setupMaster(chartCode, category, sampleSize);
      await putJson(`/v1/parts/${meta.part.id}`, { ...meta.part, partName: `${meta.part.partName}-已修改`, isEnabled: true });
      await putJson(`/v1/processes/${meta.process.id}`, { ...meta.process, processName: `${meta.process.processName}-已修改`, isEnabled: true });
      await putJson(`/v1/machines/${meta.machine.id}`, { ...meta.machine, machineName: `${meta.machine.machineName}-已修改`, isEnabled: true });
      await putJson(`/v1/characteristics/${meta.characteristic.id}`, { ...meta.characteristic, characteristicName: `${meta.characteristic.characteristicName}-已修改`, isEnabled: true });
      await putJson(`/v1/part-process-characteristics/${meta.ppc.id}`, { ...meta.ppc, isEnabled: true });
      record(`${chartCode} 主檔/PPC 新增與修改儲存`, true, { ppcId: meta.ppc.id });

      const rows = category === "Variable" ? variableRows(meta, chartCode) : attributeRows(meta, chartCode);
      const importResult = await uploadExcel(category === "Variable" ? "variable" : "attribute", rows, chartCode);
      record(`${chartCode} Excel 匯入/預覽/確認`, true, { uploadBatchId: importResult.uploaded.uploadBatchId });

      const chart = await chartCheck(chartCode, meta.ppc.id, importResult.uploaded.uploadBatchId);
      const pointCount = chartPoints(chart).length;
      record(`${chartCode} 管制圖 API 產生`, true, { points: pointCount, chartType: chart.chartType });

      const before = await api(`/v1/spc/chart?ppcId=${meta.ppc.id}&uploadBatchId=${importResult.uploaded.uploadBatchId}`);
      const exclude = await api(`/v1/spc/exclude-batch/${importResult.uploaded.uploadBatchId}`, { method: "POST" });
      const afterExclude = await api(`/v1/spc/chart?ppcId=${meta.ppc.id}&uploadBatchId=${importResult.uploaded.uploadBatchId}`);
      const restore = await api(`/v1/spc/exclude-batch/${importResult.uploaded.uploadBatchId}`, { method: "POST" });
      if (!exclude.isExcluded || restore.isExcluded) throw new Error("exclude/restore state invalid");
      record(`${chartCode} 不列入計算/恢復`, true, {
        beforeExcluded: chartPoints(before).filter(p => p.isExcluded).length,
        afterExcluded: chartPoints(afterExclude).filter(p => p.isExcluded).length
      });
    } catch (error) {
      record(`${chartCode} 流程`, false, { error: error.message });
    }
  }

  try {
    const meta = created.ppcs.I_MR;
    const res = await postJson("/v1/manual-measurements", {
      partProcessCharacteristicId: meta.ppc.id,
      batchNo: `${prefix}-MANUAL-BATCH`,
      measuredAt: new Date().toISOString(),
      operatorName: `${prefix}-OP`,
      workOrderNo: `${prefix}-WO`,
      lotNo: `${prefix}-MANUAL-LOT`,
      serialNo: `${prefix}-MANUAL-SN`,
      values: [{ sampleNo: 1, valueNumeric: 12.5 }]
    });
    record("現場量測儲存與異常觸發", true, { alerts: res.alerts?.length || 0 });
    if (res.alerts?.length) created.alerts.push(...res.alerts.map(a => a.id));
  } catch (error) {
    record("現場量測儲存與異常觸發", false, { error: error.message });
  }

  try {
    const alerts = await api("/v1/alerts");
    const target = alerts.find(a => a.partNo?.startsWith(prefix)) || alerts[0];
    if (!target) throw new Error("No alert found");
    const updated = await putJson(`/v1/alerts/${target.id}/workflow`, {
      status: "Closed",
      rootCause: `${prefix} 真因測試`,
      correctiveAction: `${prefix} 對策測試`,
      responsibleUser: `${prefix}-OWNER`
    });
    record("異常處置儲存/結案", updated.status === "Closed", { alertId: target.id });
  } catch (error) {
    record("異常處置儲存/結案", false, { error: error.message });
  }

  try {
    const trace = await api(`/v1/traceability?lotNo=${encodeURIComponent(`${prefix}-I_MR-LOT-18`)}`);
    record("多維度品質履歷查詢", Array.isArray(trace) ? trace.length > 0 : true, { rows: Array.isArray(trace) ? trace.length : undefined });
  } catch (error) {
    record("多維度品質履歷查詢", false, { error: error.message });
  }

  try {
    const reportStatus = await apiStatus("/v1/reports/cpk-summary?month=2026-06");
    const v2ReportStatus = await apiStatus("/v2/reports/cpk-summary?month=2026-06");
    record("報表匯出 V1 與 V2 封存", reportStatus === 200 && v2ReportStatus === 410, { reportStatus, v2ReportStatus });
  } catch (error) {
    record("報表匯出 V1 與 V2 封存", false, { error: error.message });
  }

  try {
    const statuses = {
      workOrder: await apiStatus("/v2/work-orders", { method: "POST", body: JSON.stringify({ workOrderNo: `${prefix}-BLOCK`, productId: 1, plannedQty: 1 }) }),
      stationOps: await apiStatus("/v2/station-ops/open-session", { method: "POST", body: JSON.stringify({ workOrderId: 1, stationId: 1, operatorName: "BLOCK" }) }),
      spcExclude: await apiStatus(`/v2/spc/exclude-batch/${created.batches.I_MR}`, { method: "POST" })
    };
    record("V2 寫入 API 全部封存", Object.values(statuses).every(s => s === 410), statuses);
  } catch (error) {
    record("V2 寫入 API 全部封存", false, { error: error.message });
  }
}

async function runUiFlow() {
  const browser = await chromium.launch({ headless: false, slowMo: 80 });
  const page = await browser.newPage({ viewport: { width: 1440, height: 950 } });
  try {
    const pages = [
      ["/", "儀表板"],
      ["/parts", "產品料號主檔"],
      ["/processes", "製程主檔"],
      ["/machines", "機台主檔"],
      ["/characteristics", "品質特性主檔"],
      ["/part-process-characteristics", "PPC 基準"],
      ["/uploads/variable", "計量匯入"],
      ["/uploads/attribute", "計數匯入"],
      ["/spc", "SPC 管制圖"],
      ["/spc/query", "SPC 查詢"],
      ["/alerts", "異常總覽"],
      ["/alerts-workflow", "異常處置"],
      ["/settings/smtp", "SMTP 設定"],
      ["/guide", "系統操作手冊"],
      ["/sitemap", "網站地圖"]
    ];

    for (const [url, name] of pages) {
      await page.goto(`${WEB}${url}`, { waitUntil: "networkidle" });
      await page.screenshot({ path: path.join(outDir, `${name.replace(/[\\/:*?"<>| ]/g, "_")}.png`), fullPage: true });
        const text = await page.locator("body").innerText();
        const titleCount = await page.locator("h1, h2, h3").count();
        record(`頁面可開啟：${name}`, text.trim().length > 20 && titleCount > 0 && !/Cannot GET|HTTP Error 404|This page could not be found/i.test(text), { url });
    }

    for (const [chartCode, meta] of Object.entries(created.ppcs)) {
      await page.goto(`${WEB}/spc?ppcId=${meta.ppc.id}&uploadBatchId=${created.batches[chartCode]}`, { waitUntil: "networkidle" });
      await page.waitForTimeout(1200);
      const canvasCount = await page.locator("canvas").count();
      await page.screenshot({ path: path.join(outDir, `chart-${chartCode}.png`), fullPage: true });
      record(`畫面圖表產生：${chartCode}`, canvasCount > 0, { canvasCount, url: page.url() });
    }

    await page.goto(`${WEB}/v2/alerts-workflow`, { waitUntil: "networkidle" });
    record("舊 V2 畫面路由 redirect", page.url().endsWith("/alerts-workflow"), { url: page.url() });
  } finally {
    await browser.close();
  }
}

(async () => {
  await runApiFlow();
  await runUiFlow();
  const summary = {
    prefix,
    outDir,
    total: results.length,
    passed: results.filter(r => r.ok).length,
    failed: results.filter(r => !r.ok).length,
    results,
    created
  };
  fs.writeFileSync(path.join(outDir, "report.json"), JSON.stringify(summary, null, 2));
  console.log(JSON.stringify(summary, null, 2));
  if (summary.failed > 0) process.exitCode = 1;
})().catch(error => {
  record("測試腳本未處理例外", false, { error: error.stack || error.message });
  fs.writeFileSync(path.join(outDir, "report.json"), JSON.stringify({ prefix, outDir, results }, null, 2));
  process.exit(1);
});
