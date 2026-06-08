import playwright from "../frontend/mes-spc-web/node_modules/playwright/index.js";
import fs from "node:fs/promises";
import path from "node:path";

const { chromium } = playwright;
const webBase = "http://172.16.110.27:8083";
const apiBase = "http://172.16.110.27:8082/api";
const stamp = new Date().toISOString().replace(/[-:T.Z]/g, "").slice(0, 17);
const outDir = path.resolve("test-artifacts", `p-chart-${stamp}`);
await fs.mkdir(outDir, { recursive: true });

const browser = await chromium.launch({ headless: false, slowMo: 250 });
const page = await browser.newPage({ viewport: { width: 1440, height: 950 } });
page.setDefaultTimeout(90_000);

const result = {
  webBase,
  apiBase,
  outDir,
  stamp,
  master: {},
  upload: {},
  chart: {},
  exclude: {},
  screenshots: [],
  consoleErrors: [],
  pageErrors: [],
};

page.on("console", (msg) => {
  if (msg.type() === "error") result.consoleErrors.push(msg.text());
});
page.on("pageerror", (error) => result.pageErrors.push(error.message));

async function api(method, pathName, body) {
  const response = await page.request.fetch(`${apiBase}${pathName}`, {
    method,
    data: body,
    headers: body ? { "Content-Type": "application/json" } : undefined,
    timeout: 90_000,
  });
  const text = await response.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }
  return { status: response.status(), ok: response.ok(), data, text };
}

function unique(prefix) {
  return `E2E-P-${stamp}-${prefix}`;
}

async function getChartType(code) {
  const res = await api("GET", "/v1/control-chart-types");
  const match = (res.data || []).find((x) => x.chartTypeCode === code);
  if (!match) throw new Error(`Missing chart type ${code}`);
  return match;
}

async function firstRuleGroup() {
  const res = await api("GET", "/v1/spc-rule-groups");
  return (res.data || [])[0];
}

async function precreateMaster() {
  const type = await getChartType("P");
  const rule = await firstRuleGroup();
  const key = unique("PCHART");
  const part = (await api("POST", "/v1/parts", {
    partNo: `${key}-PART`,
    partName: `${key} Part`,
    isEnabled: true,
  })).data;
  const process = (await api("POST", "/v1/processes", {
    processCode: `${key}-PROC`,
    processName: `${key} Process`,
    isEnabled: true,
  })).data;
  const machine = (await api("POST", "/v1/machines", {
    machineCode: `${key}-M01`,
    machineName: `${key} Machine`,
    processId: process.id,
    isEnabled: true,
  })).data;
  const characteristic = (await api("POST", "/v1/characteristics", {
    characteristicCode: `${key}-CHAR`,
    characteristicName: `${key} P Chart Defect Rate`,
    dataCategory: "Attribute",
    defaultChartTypeId: type.id,
    isSpcEnabled: true,
    isEnabled: true,
  })).data;
  const ppc = (await api("POST", "/v1/part-process-characteristics", {
    partId: part.id,
    processId: process.id,
    characteristicId: characteristic.id,
    usl: 0.1,
    lsl: 0,
    ucl: 0.08,
    cl: 0.02,
    lcl: 0,
    targetValue: 0.02,
    sampleSize: 1,
    chartTypeId: type.id,
    ruleGroupId: rule?.id,
    isRequired: true,
    isEnabled: true,
  })).data;

  result.master = { type, part, process, machine, characteristic, ppc };
  return result.master;
}

async function uploadAndConfirm(rows) {
  const upload = await api("POST", "/v1/uploads/attribute", rows);
  const batchId = upload.data?.uploadBatchId;
  const preview = await api("GET", `/v1/uploads/${batchId}/preview`);
  const confirm = await api("POST", `/v1/uploads/${batchId}/confirm`);
  result.upload = { batchId, upload, preview, confirm };
  return result.upload;
}

function pointsOf(response) {
  return response?.data?.chartData?.points || [];
}

function summarizePoints(points) {
  return points.map((p, index) => ({
    index: index + 1,
    lotNo: p.lotNo,
    value: p.value,
    inspectedQty: p.inspectedQty,
    defectQty: p.defectQty,
    expectedP: p.inspectedQty ? p.defectQty / p.inspectedQty : null,
    outOfControl: !!p.outOfControl,
    outOfSpec: !!p.outOfSpec,
    isExcluded: !!p.isExcluded,
  }));
}

async function main() {
  const master = await precreateMaster();
  const samples = [
    { lot: "LOT-01", inspected: 100, defects: 2 },
    { lot: "LOT-02", inspected: 120, defects: 3 },
    { lot: "LOT-03", inspected: 90, defects: 1 },
    { lot: "LOT-04", inspected: 110, defects: 2 },
    { lot: "LOT-05", inspected: 100, defects: 3 },
    { lot: "LOT-06", inspected: 130, defects: 2 },
    { lot: "LOT-07", inspected: 95, defects: 2 },
    { lot: "LOT-08", inspected: 125, defects: 4 },
    { lot: "LOT-09", inspected: 100, defects: 18 },
    { lot: "LOT-10", inspected: 115, defects: 2 },
  ];

  const rows = samples.map((x, idx) => {
    const measuredAt = new Date("2026-06-08T09:00:00.000Z");
    measuredAt.setMinutes(measuredAt.getMinutes() + idx * 10);
    return {
      PartNo: master.part.partNo,
      ProcessCode: master.process.processCode,
      MachineCode: master.machine.machineCode,
      CharacteristicCode: master.characteristic.characteristicCode,
      CharacteristicName: master.characteristic.characteristicName,
      MeasuredAt: measuredAt.toISOString(),
      Operator: "playwright",
      LotNo: `${unique("PCHART")}-${x.lot}`,
      SampleNo: String(idx + 1),
      InspectedQty: String(x.inspected),
      DefectQty: String(x.defects),
      UnitCount: String(x.inspected),
      USL: "0.1",
      LSL: "0",
    };
  });

  const upload = await uploadAndConfirm(rows);
  const before = await api("GET", `/v1/spc/chart?ppcId=${master.ppc.id}&uploadBatchId=${upload.batchId}`);
  const beforePoints = pointsOf(before);
  const target = beforePoints.find((p) => String(p.lotNo || "").includes("LOT-09")) || beforePoints.find((p) => p.outOfControl) || beforePoints[8];

  const toggle = await api("POST", `/v2/spc/exclude-batch/${upload.batchId}`);
  const afterExclude = await api("GET", `/v1/spc/chart?ppcId=${master.ppc.id}&uploadBatchId=${upload.batchId}`);
  const restore = await api("POST", `/v2/spc/exclude-batch/${upload.batchId}`);
  const afterRestore = await api("GET", `/v1/spc/chart?ppcId=${master.ppc.id}&uploadBatchId=${upload.batchId}`);

  result.chart = {
    beforeStatus: before.status,
    beforeChartType: before.data?.chartType,
    pointCount: beforePoints.length,
    points: summarizePoints(beforePoints),
    outOfControlCount: beforePoints.filter((p) => p.outOfControl).length,
    targetLotNo: target?.lotNo,
    targetValue: target?.value,
    targetOutOfControl: !!target?.outOfControl,
    valueChecks: summarizePoints(beforePoints).map((p) => ({
      lotNo: p.lotNo,
      value: p.value,
      expectedP: p.expectedP,
      delta: p.value != null && p.expectedP != null ? Math.abs(p.value - p.expectedP) : null,
    })),
  };
  result.exclude = {
    toggleStatus: toggle.status,
    toggleIsExcluded: toggle.data?.isExcluded,
    afterStatus: afterExclude.status,
    afterExcludedCount: pointsOf(afterExclude).filter((p) => p.isExcluded).length,
    afterOutOfControlCount: pointsOf(afterExclude).filter((p) => p.outOfControl).length,
    restoreStatus: restore.status,
    restoreIsExcluded: restore.data?.isExcluded,
    afterRestoreStatus: afterRestore.status,
    afterRestoreOutOfControlCount: pointsOf(afterRestore).filter((p) => p.outOfControl).length,
  };

  await page.goto(`${webBase}/spc?ppcId=${master.ppc.id}&uploadBatchId=${upload.batchId}`, { waitUntil: "networkidle" });
  await page.screenshot({ path: path.join(outDir, "01-p-chart-page.png"), fullPage: true });
  result.screenshots.push(path.join(outDir, "01-p-chart-page.png"));

  await page.goto(`${webBase}/alerts`, { waitUntil: "networkidle" });
  await page.screenshot({ path: path.join(outDir, "02-alerts-page.png"), fullPage: true });
  result.screenshots.push(path.join(outDir, "02-alerts-page.png"));

  await fs.writeFile(path.join(outDir, "result.json"), JSON.stringify(result, null, 2), "utf8");
  console.log(JSON.stringify(result, null, 2));
}

try {
  await main();
} finally {
  await browser.close();
}
