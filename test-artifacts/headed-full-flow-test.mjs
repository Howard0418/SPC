import playwright from "../frontend/mes-spc-web/node_modules/playwright/index.js";
import fs from "node:fs/promises";
import path from "node:path";

const { chromium } = playwright;
const webBase = "http://172.16.110.27:8083";
const apiBase = "http://172.16.110.27:8082/api";
const stamp = new Date().toISOString().replace(/[-:T.Z]/g, "").slice(0, 17);
const outDir = path.resolve("test-artifacts", `headed-full-flow-${stamp}`);
await fs.mkdir(outDir, { recursive: true });

const browser = await chromium.launch({ headless: false, slowMo: 250 });
const page = await browser.newPage({ viewport: { width: 1440, height: 950 } });
page.setDefaultTimeout(90_000);

const result = {
  webBase,
  apiBase,
  outDir,
  uiUploads: [],
  chartMatrix: [],
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
  return `E2E-PW-${stamp}-${prefix}`;
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

async function precreateMaster({ chartCode, dataCategory, limits, sampleSize }) {
  const type = await getChartType(chartCode);
  const rule = await firstRuleGroup();
  const key = unique(chartCode);
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
    characteristicName: `${key} Characteristic`,
    dataCategory,
    defaultChartTypeId: type.id,
    isSpcEnabled: true,
    isEnabled: true,
  })).data;
  const ppc = (await api("POST", "/v1/part-process-characteristics", {
    partId: part.id,
    processId: process.id,
    characteristicId: characteristic.id,
    usl: limits.usl,
    lsl: limits.lsl,
    ucl: limits.ucl,
    cl: limits.cl,
    lcl: limits.lcl,
    targetValue: limits.target,
    sampleSize,
    chartTypeId: type.id,
    ruleGroupId: rule?.id,
    isRequired: true,
    isEnabled: true,
  })).data;

  return { type, part, process, machine, characteristic, ppc };
}

async function uploadAndConfirm(uploadType, rows) {
  const uploadPath = uploadType === "Attribute" ? "/v1/uploads/attribute" : "/v1/uploads/variable";
  const upload = await api("POST", uploadPath, rows);
  const batchId = upload.data?.uploadBatchId;
  let preview = await api("GET", `/v1/uploads/${batchId}/preview`);
  let confirm;
  try {
    confirm = await api("POST", `/v1/uploads/${batchId}/confirm`);
  } catch (error) {
    if (!String(error?.message || "").includes("Timeout")) throw error;

    for (let attempt = 0; attempt < 18; attempt += 1) {
      await page.waitForTimeout(5_000);
      preview = await api("GET", `/v1/uploads/${batchId}/preview`);
      if (preview.data?.batch?.importStatus === "Imported") {
        confirm = {
          status: 200,
          ok: true,
          data: {
            batch: preview.data.batch,
            imported: null,
            alertCount: null,
            timedOutButImported: true,
          },
          text: "",
        };
        break;
      }
    }

    if (!confirm) throw error;
  }
  return { upload, preview, confirm, batchId };
}

function chartPoints(chart) {
  return chart?.data?.chartData?.points || [];
}

async function runChartExcludeCase(config) {
  const master = await precreateMaster(config);
  const measuredBase = "2026-06-05 13:00:00";
  const rows = [];

  if (config.dataCategory === "Variable") {
    let groupIndex = 0;
    for (const group of config.groups) {
      groupIndex += 1;
      let sampleNo = 0;
      const measuredAt = new Date(measuredBase);
      measuredAt.setMinutes(measuredAt.getMinutes() + groupIndex * 10);
      for (const value of group) {
        sampleNo += 1;
        rows.push({
          PartNo: master.part.partNo,
          ProcessCode: master.process.processCode,
          MachineCode: master.machine.machineCode,
          CharacteristicCode: master.characteristic.characteristicCode,
          CharacteristicName: master.characteristic.characteristicName,
          MeasuredValue: String(value),
          MeasuredAt: measuredAt.toISOString(),
          Operator: "playwright",
          LotNo: `${unique(config.chartCode)}-LOT`,
          SampleNo: String(sampleNo),
          USL: String(config.limits.usl),
          LSL: String(config.limits.lsl),
        });
      }
    }
  } else {
    let i = 0;
    for (const item of config.attributeRows) {
      i += 1;
      const measuredAt = new Date(measuredBase);
      measuredAt.setMinutes(measuredAt.getMinutes() + i * 10);
      rows.push({
        PartNo: master.part.partNo,
        ProcessCode: master.process.processCode,
        MachineCode: master.machine.machineCode,
        CharacteristicCode: master.characteristic.characteristicCode,
        CharacteristicName: master.characteristic.characteristicName,
        MeasuredAt: measuredAt.toISOString(),
        Operator: "playwright",
        LotNo: `${unique(config.chartCode)}-LOT`,
        SampleNo: String(i),
        USL: String(config.limits.usl),
        LSL: String(config.limits.lsl),
        ...Object.fromEntries(Object.entries(item).map(([k, v]) => [k, String(v)])),
      });
    }
  }

  const uploadInfo = await uploadAndConfirm(config.dataCategory, rows);
  const before = await api("GET", `/v1/spc/chart?ppcId=${master.ppc.id}&uploadBatchId=${uploadInfo.batchId}`);
  const toggle = await api("POST", `/v2/spc/exclude-batch/${uploadInfo.batchId}`);
  const after = await api("GET", `/v1/spc/chart?ppcId=${master.ppc.id}&uploadBatchId=${uploadInfo.batchId}`);
  const restore = await api("POST", `/v2/spc/exclude-batch/${uploadInfo.batchId}`);

  const beforePoints = chartPoints(before);
  const afterPoints = chartPoints(after);
  return {
    chartCode: config.chartCode,
    dataCategory: config.dataCategory,
    batchId: uploadInfo.batchId,
    ppcId: master.ppc.id,
    uploadStatus: uploadInfo.upload.status,
    validRows: uploadInfo.preview.data?.batch?.validRows,
    errorRows: uploadInfo.preview.data?.batch?.errorRows,
    imported: uploadInfo.confirm.data?.imported,
    confirmAlertCount: uploadInfo.confirm.data?.alertCount,
    chartStatusBefore: before.status,
    returnedChartType: before.data?.chartType,
    pointCountBefore: beforePoints.length,
    outOfControlBefore: beforePoints.filter((p) => p.outOfControl).length,
    excludeStatus: toggle.status,
    excludeIsExcluded: toggle.data?.isExcluded,
    chartStatusAfter: after.status,
    excludedAfter: afterPoints.filter((p) => p.isExcluded).length,
    outOfControlAfter: afterPoints.filter((p) => p.outOfControl).length,
    restoreStatus: restore.status,
    restoreIsExcluded: restore.data?.isExcluded,
  };
}

async function openFromSidebar(navText) {
  await page.goto(webBase, { waitUntil: "domcontentloaded", timeout: 30000 });
  await page.waitForTimeout(1000);
  const link = page.getByText(navText, { exact: true });
  await link.waitFor({ state: "visible", timeout: 30000 });
  await link.click();
  await page.waitForTimeout(1500);
}

async function fillJsonUpload(route, navText, payload, label, screenshotPrefix) {
  await openFromSidebar(navText);
  await page.waitForTimeout(1200);
  await page.getByText("JSON 格式直接送出", { exact: false }).click();
  await page.locator("textarea").fill(JSON.stringify(payload));
  await page.screenshot({ path: path.join(outDir, `${screenshotPrefix}-01-json.png`), fullPage: true });
  await page.getByText("送出 JSON 並進入 Stage 2 檢核", { exact: true }).click();
  await page.waitForURL(`${webBase}/uploads/**/preview`, { timeout: 30000 });
  await page.waitForTimeout(1500);
  const batchId = page.url().match(/\/uploads\/([^/]+)\/preview/)?.[1] ?? null;
  await page.screenshot({ path: path.join(outDir, `${screenshotPrefix}-02-preview.png`), fullPage: true });
  const confirmButton = page.getByText("確認轉入正式 SPC 運算", { exact: true });
  if (await confirmButton.count() === 1) {
    await confirmButton.click();
    await page.waitForTimeout(2000);
  }
  await page.screenshot({ path: path.join(outDir, `${screenshotPrefix}-03-confirmed.png`), fullPage: true });
  const batchStatus = batchId ? await api("GET", `/v1/uploads/${batchId}/preview`) : null;
  result.uiUploads.push({
    label,
    route,
    batchId,
    urlAfterConfirm: page.url(),
    batchStatus: batchStatus?.data?.batch?.importStatus,
    validRows: batchStatus?.data?.batch?.validRows,
    errorRows: batchStatus?.data?.batch?.errorRows,
    screenshots: [
      path.join(outDir, `${screenshotPrefix}-01-json.png`),
      path.join(outDir, `${screenshotPrefix}-02-preview.png`),
      path.join(outDir, `${screenshotPrefix}-03-confirmed.png`),
    ],
  });
}

try {
  await page.goto(webBase, { waitUntil: "domcontentloaded", timeout: 30000 });
  await page.waitForTimeout(1000);
  await page.screenshot({ path: path.join(outDir, "00-home.png"), fullPage: true });

  const uiVarKey = unique("UI-VAR");
  await fillJsonUpload("/uploads/variable", "計量型資料匯入", [
    {
      PartNo: `${uiVarKey}-PART`,
      ProcessCode: `${uiVarKey}-PROC`,
      MachineCode: `${uiVarKey}-M01`,
      CharacteristicCode: `${uiVarKey}-CHAR`,
      CharacteristicName: "UI Variable Upload",
      MeasuredValue: "10.1",
      MeasuredAt: "2026-06-05T11:00:00",
      Operator: "playwright-ui",
      LotNo: `${uiVarKey}-LOT`,
      SampleNo: "1",
      USL: "15",
      LSL: "5",
    },
  ], "計量型 UI JSON 匯入", "01-variable-upload");

  const uiAttrKey = unique("UI-ATTR");
  await fillJsonUpload("/uploads/attribute", "計數型資料匯入", [
    {
      PartNo: `${uiAttrKey}-PART`,
      ProcessCode: `${uiAttrKey}-PROC`,
      MachineCode: `${uiAttrKey}-M01`,
      CharacteristicCode: `${uiAttrKey}-CHAR`,
      CharacteristicName: "UI Attribute Upload",
      InspectedQty: "100",
      DefectQty: "2",
      DefectCount: "2",
      UnitCount: "100",
      MeasuredAt: "2026-06-05T11:30:00",
      Operator: "playwright-ui",
      LotNo: `${uiAttrKey}-LOT`,
      SampleNo: "1",
      USL: "999",
      LSL: "0",
    },
  ], "計數型 UI JSON 匯入", "02-attribute-upload");

  const cases = [
    {
      chartCode: "I_MR",
      dataCategory: "Variable",
      limits: { usl: 15, lsl: 5, ucl: 15, cl: 10, lcl: 5, target: 10 },
      sampleSize: 1,
      groups: [[10], [10.2], [9.9], [18]],
    },
    {
      chartCode: "XBAR_R",
      dataCategory: "Variable",
      limits: { usl: 15, lsl: 5, ucl: 15, cl: 10, lcl: 5, target: 10 },
      sampleSize: 5,
      groups: [[10, 10.1, 9.9, 10.2, 10], [10.1, 10, 10.2, 9.9, 10], [18, 18.1, 17.9, 18.2, 18]],
    },
    {
      chartCode: "XBAR_S",
      dataCategory: "Variable",
      limits: { usl: 15, lsl: 5, ucl: 15, cl: 10, lcl: 5, target: 10 },
      sampleSize: 5,
      groups: [[10, 10.1, 9.9, 10.2, 10], [10.1, 10, 10.2, 9.9, 10], [18, 18.1, 17.9, 18.2, 18]],
    },
    {
      chartCode: "P",
      dataCategory: "Attribute",
      limits: { usl: 999, lsl: 0, ucl: 0.05, cl: 0.02, lcl: 0, target: 0.02 },
      sampleSize: 1,
      attributeRows: [{ InspectedQty: 100, DefectQty: 1 }, { InspectedQty: 100, DefectQty: 2 }, { InspectedQty: 100, DefectQty: 12 }],
    },
    {
      chartCode: "NP",
      dataCategory: "Attribute",
      limits: { usl: 999, lsl: 0, ucl: 5, cl: 2, lcl: 0, target: 2 },
      sampleSize: 1,
      attributeRows: [{ InspectedQty: 100, DefectQty: 1 }, { InspectedQty: 100, DefectQty: 2 }, { InspectedQty: 100, DefectQty: 12 }],
    },
    {
      chartCode: "C",
      dataCategory: "Attribute",
      limits: { usl: 999, lsl: 0, ucl: 5, cl: 2, lcl: 0, target: 2 },
      sampleSize: 1,
      attributeRows: [{ DefectCount: 1 }, { DefectCount: 2 }, { DefectCount: 12 }],
    },
    {
      chartCode: "U",
      dataCategory: "Attribute",
      limits: { usl: 999, lsl: 0, ucl: 0.05, cl: 0.02, lcl: 0, target: 0.02 },
      sampleSize: 1,
      attributeRows: [{ UnitCount: 100, DefectCount: 1 }, { UnitCount: 100, DefectCount: 2 }, { UnitCount: 100, DefectCount: 12 }],
    },
  ];

  for (const item of cases) {
    result.chartMatrix.push(await runChartExcludeCase(item));
  }

  await page.goto(`${webBase}/alerts`, { waitUntil: "domcontentloaded", timeout: 30000 });
  await page.waitForTimeout(1200);
  await page.screenshot({ path: path.join(outDir, "99-alerts-after-flow.png"), fullPage: true });

  await fs.writeFile(path.join(outDir, "result.json"), JSON.stringify(result, null, 2), "utf8");
  console.log(JSON.stringify(result, null, 2));
} finally {
  await page.waitForTimeout(3000);
  await browser.close();
}
