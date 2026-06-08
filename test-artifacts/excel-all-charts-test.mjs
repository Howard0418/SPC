import playwright from "../frontend/mes-spc-web/node_modules/playwright/index.js";
import * as XLSX from "../frontend/mes-spc-web/node_modules/xlsx/xlsx.mjs";
import fs from "node:fs/promises";
import path from "node:path";

const { chromium } = playwright;
const webBase = "http://172.16.110.27:8083";
const apiBase = "http://172.16.110.27:8082/api";
const stamp = new Date().toISOString().replace(/[-:T.Z]/g, "").slice(0, 17);
const outDir = path.resolve("test-artifacts", `excel-all-charts-${stamp}`);
await fs.mkdir(outDir, { recursive: true });

const browser = await chromium.launch({ headless: false, slowMo: 200 });
const page = await browser.newPage({ viewport: { width: 1440, height: 950 } });
page.setDefaultTimeout(90_000);

const result = {
  webBase,
  apiBase,
  outDir,
  stamp,
  charts: [],
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

async function uploadExcel(pathName, filePath) {
  const buffer = await fs.readFile(filePath);
  const response = await page.request.fetch(`${apiBase}${pathName}`, {
    method: "POST",
    multipart: {
      file: {
        name: path.basename(filePath),
        mimeType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        buffer,
      },
    },
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

function unique(chartCode, suffix) {
  return `E2E-XLS-${stamp}-${chartCode}-${suffix}`;
}

async function writeWorkbook(filePath, rows) {
  const wb = XLSX.utils.book_new();
  const ws = XLSX.utils.json_to_sheet(rows);
  XLSX.utils.book_append_sheet(wb, ws, "Data");
  const buffer = XLSX.write(wb, { type: "buffer", bookType: "xlsx" });
  await fs.writeFile(filePath, buffer);
}

async function precreateMaster(config) {
  const type = await getChartType(config.chartCode);
  const rule = await firstRuleGroup();
  const part = (await api("POST", "/v1/parts", {
    partNo: unique(config.chartCode, "PART"),
    partName: unique(config.chartCode, "Part"),
    isEnabled: true,
  })).data;
  const process = (await api("POST", "/v1/processes", {
    processCode: unique(config.chartCode, "PROC"),
    processName: unique(config.chartCode, "Process"),
    isEnabled: true,
  })).data;
  const machine = (await api("POST", "/v1/machines", {
    machineCode: unique(config.chartCode, "M01"),
    machineName: unique(config.chartCode, "Machine"),
    processId: process.id,
    isEnabled: true,
  })).data;
  const characteristic = (await api("POST", "/v1/characteristics", {
    characteristicCode: unique(config.chartCode, "CHAR"),
    characteristicName: unique(config.chartCode, "Characteristic"),
    dataCategory: config.dataCategory,
    defaultChartTypeId: type.id,
    isSpcEnabled: true,
    isEnabled: true,
  })).data;
  const ppc = (await api("POST", "/v1/part-process-characteristics", {
    partId: part.id,
    processId: process.id,
    characteristicId: characteristic.id,
    usl: config.limits.usl,
    lsl: config.limits.lsl,
    ucl: config.limits.ucl,
    cl: config.limits.cl,
    lcl: config.limits.lcl,
    targetValue: config.limits.target,
    sampleSize: config.sampleSize,
    chartTypeId: type.id,
    ruleGroupId: rule?.id,
    isRequired: true,
    isEnabled: true,
  })).data;

  return { type, part, process, machine, characteristic, ppc };
}

async function confirmUpload(batchId) {
  let preview = await api("GET", `/v1/uploads/${batchId}/preview`);
  let confirm = await api("POST", `/v1/uploads/${batchId}/confirm`);
  if (!confirm.ok) {
    for (let i = 0; i < 18; i += 1) {
      await page.waitForTimeout(5_000);
      preview = await api("GET", `/v1/uploads/${batchId}/preview`);
      if (preview.data?.batch?.importStatus === "Imported") break;
    }
  }
  return { preview, confirm };
}

function variableRows(master, config) {
  const rows = [];
  const base = new Date("2026-06-08T11:00:00.000Z");
  if (config.chartCode === "I_MR") {
    config.values.forEach((value, idx) => {
      const measuredAt = new Date(base);
      measuredAt.setMinutes(measuredAt.getMinutes() + idx * 10);
      rows.push({
        PartNo: master.part.partNo,
        ProcessCode: master.process.processCode,
        MachineCode: master.machine.machineCode,
        CharacteristicCode: master.characteristic.characteristicCode,
        CharacteristicName: master.characteristic.characteristicName,
        MeasuredValue: value,
        MeasuredAt: measuredAt.toISOString(),
        Operator: "excel-user",
        LotNo: unique(config.chartCode, `LOT-${String(idx + 1).padStart(2, "0")}`),
        SampleNo: 1,
        USL: config.limits.usl,
        LSL: config.limits.lsl,
      });
    });
    return rows;
  }

  config.groups.forEach((group, groupIndex) => {
    const measuredAt = new Date(base);
    measuredAt.setMinutes(measuredAt.getMinutes() + groupIndex * 10);
    group.forEach((value, sampleIndex) => {
      rows.push({
        PartNo: master.part.partNo,
        ProcessCode: master.process.processCode,
        MachineCode: master.machine.machineCode,
        CharacteristicCode: master.characteristic.characteristicCode,
        CharacteristicName: master.characteristic.characteristicName,
        MeasuredValue: value,
        MeasuredAt: measuredAt.toISOString(),
        Operator: "excel-user",
        LotNo: unique(config.chartCode, `LOT-${String(groupIndex + 1).padStart(2, "0")}`),
        SampleNo: sampleIndex + 1,
        USL: config.limits.usl,
        LSL: config.limits.lsl,
      });
    });
  });
  return rows;
}

function attributeRows(master, config) {
  const base = new Date("2026-06-08T12:00:00.000Z");
  return config.rows.map((x, idx) => {
    const measuredAt = new Date(base);
    measuredAt.setMinutes(measuredAt.getMinutes() + idx * 10);
    return {
      PartNo: master.part.partNo,
      ProcessCode: master.process.processCode,
      MachineCode: master.machine.machineCode,
      CharacteristicCode: master.characteristic.characteristicCode,
      CharacteristicName: master.characteristic.characteristicName,
      MeasuredAt: measuredAt.toISOString(),
      Operator: "excel-user",
      LotNo: unique(config.chartCode, `LOT-${String(idx + 1).padStart(2, "0")}`),
      SampleNo: idx + 1,
      InspectedQty: x.inspectedQty ?? "",
      DefectQty: x.defectQty ?? "",
      DefectCount: x.defectCount ?? "",
      UnitCount: x.unitCount ?? "",
      USL: config.limits.usl,
      LSL: config.limits.lsl,
    };
  });
}

function chartPoints(response) {
  return response?.data?.chartData?.points || [];
}

async function runCase(config, index) {
  const master = await precreateMaster(config);
  const rows = config.dataCategory === "Variable" ? variableRows(master, config) : attributeRows(master, config);
  const filePath = path.join(outDir, `${String(index).padStart(2, "0")}-${config.chartCode}.xlsx`);
  await writeWorkbook(filePath, rows);

  const upload = await uploadExcel(config.dataCategory === "Variable" ? "/v1/uploads/variable/excel" : "/v1/uploads/attribute/excel", filePath);
  const batchId = upload.data?.uploadBatchId;
  const { preview, confirm } = await confirmUpload(batchId);
  const chartApiPath = `/v1/spc/chart?ppcId=${master.ppc.id}&uploadBatchId=${batchId}`;
  const chart = await api("GET", chartApiPath);
  const points = chartPoints(chart);

  const url = `${webBase}/spc`;
  await page.goto(url, { waitUntil: "networkidle" });
  const screenshot = path.join(outDir, `${String(index).padStart(2, "0")}-${config.chartCode}-spc-page.png`);
  await page.screenshot({ path: screenshot, fullPage: true });

  return {
    chartCode: config.chartCode,
    expectedChart: config.expectedChart,
    dataCategory: config.dataCategory,
    excelFile: filePath,
    screenshot,
    master: {
      ppcId: master.ppc.id,
      partNo: master.part.partNo,
      processCode: master.process.processCode,
      machineCode: master.machine.machineCode,
      characteristicCode: master.characteristic.characteristicCode,
    },
    query: {
      apiUrl: `${apiBase}${chartApiPath}`,
      uiInstruction: `SPC 查詢 / 管制圖：選產品 ${master.part.partNo}，製程 ${master.process.processCode}，檢驗項目 ${master.characteristic.characteristicCode}`,
    },
    upload: {
      status: upload.status,
      batchId,
      importStatusBeforeConfirm: upload.data?.importStatus,
      totalRows: upload.data?.totalRows,
      validRows: upload.data?.validRows,
      errorRows: upload.data?.errorRows,
      previewStatus: preview.status,
      confirmStatus: confirm.status,
      confirmedStatus: confirm.data?.batch?.importStatus ?? preview.data?.batch?.importStatus,
      imported: confirm.data?.imported,
      alertCount: confirm.data?.alertCount,
    },
    chart: {
      status: chart.status,
      returnedChartType: chart.data?.chartType,
      pointCount: points.length,
      outOfControlCount: points.filter((p) => p.outOfControl).length,
      firstPoint: points[0] ?? null,
    },
  };
}

const configs = [
  {
    chartCode: "I_MR",
    expectedChart: "I-MR",
    dataCategory: "Variable",
    sampleSize: 1,
    limits: { usl: 10, lsl: 0, ucl: 7.5, cl: 5, lcl: 2.5, target: 5 },
    values: [5.1, 5.2, 4.9, 5.0, 5.3, 4.8, 5.1, 8.9],
  },
  {
    chartCode: "XBAR_R",
    expectedChart: "XBAR_R",
    dataCategory: "Variable",
    sampleSize: 5,
    limits: { usl: 20, lsl: 0, ucl: 15, cl: 10, lcl: 5, target: 10 },
    groups: [[10.1, 10.2, 9.9, 10.0, 10.1], [10.4, 10.3, 10.2, 10.5, 10.1], [14.8, 15.2, 15.1, 14.9, 15.0]],
  },
  {
    chartCode: "XBAR_S",
    expectedChart: "XBAR_S",
    dataCategory: "Variable",
    sampleSize: 5,
    limits: { usl: 20, lsl: 0, ucl: 15, cl: 10, lcl: 5, target: 10 },
    groups: [[10.0, 10.1, 9.8, 10.2, 10.0], [10.3, 10.4, 10.2, 10.5, 10.3], [14.9, 15.1, 15.2, 15.0, 15.3]],
  },
  {
    chartCode: "P",
    expectedChart: "P_CHART",
    dataCategory: "Attribute",
    sampleSize: 1,
    limits: { usl: 0.1, lsl: 0, ucl: 0.08, cl: 0.02, lcl: 0, target: 0.02 },
    rows: [{ inspectedQty: 100, defectQty: 2 }, { inspectedQty: 120, defectQty: 3 }, { inspectedQty: 90, defectQty: 1 }, { inspectedQty: 100, defectQty: 18 }],
  },
  {
    chartCode: "NP",
    expectedChart: "NP_CHART",
    dataCategory: "Attribute",
    sampleSize: 1,
    limits: { usl: 20, lsl: 0, ucl: 12, cl: 3, lcl: 0, target: 3 },
    rows: [{ inspectedQty: 100, defectQty: 2 }, { inspectedQty: 100, defectQty: 3 }, { inspectedQty: 100, defectQty: 2 }, { inspectedQty: 100, defectQty: 18 }],
  },
  {
    chartCode: "C",
    expectedChart: "C_CHART",
    dataCategory: "Attribute",
    sampleSize: 1,
    limits: { usl: 30, lsl: 0, ucl: 15, cl: 4, lcl: 0, target: 4 },
    rows: [{ defectCount: 3 }, { defectCount: 4 }, { defectCount: 5 }, { defectCount: 22 }],
  },
  {
    chartCode: "U",
    expectedChart: "U_CHART",
    dataCategory: "Attribute",
    sampleSize: 1,
    limits: { usl: 1, lsl: 0, ucl: 0.5, cl: 0.08, lcl: 0, target: 0.08 },
    rows: [{ defectCount: 5, unitCount: 100 }, { defectCount: 7, unitCount: 120 }, { defectCount: 4, unitCount: 80 }, { defectCount: 80, unitCount: 100 }],
  },
];

try {
  for (let i = 0; i < configs.length; i += 1) {
    const item = await runCase(configs[i], i + 1);
    result.charts.push(item);
  }
  await fs.writeFile(path.join(outDir, "result.json"), JSON.stringify(result, null, 2), "utf8");
  console.log(JSON.stringify(result, null, 2));
} finally {
  await browser.close();
}
