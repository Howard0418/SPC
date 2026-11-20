import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { runInNewContext } from "node:vm";

const source = readFileSync(new URL("../src/views/SpcQueryView.vue", import.meta.url), "utf8");
const handler = source.slice(source.search(/(?:async )?function exportCpkMaster\(\)/), source.indexOf("async function executeQuery()"));

function exportContext(get) {
  const link = { click() { this.clicked = true; } };
  const context = {
    api: { get },
    exportMonth: { value: "2026-10" },
    locale: { value: "zh-TW" },
    normalizeLocale: value => value,
    err: { value: "" },
    exporting: { value: false },
    document: { createElement: () => link },
    URL: { createObjectURL: () => "blob:report", revokeObjectURL: () => {} },
    getApiErrorDisplay: () => "download error"
  };
  return { link, context };
}

test("uses the authenticated API client and downloads the selected month", async () => {
  const { link, context } = exportContext(async (url, options) => {
    assert.equal(url, "/v1/reports/cpk-summary");
    assert.equal(options.params.month, "2026-10");
    assert.equal(options.params.uiLocale, "zh-TW");
    assert.equal(options.responseType, "blob");
    return { data: new Blob() };
  });
  await runInNewContext(handler + ";exportCpkMaster()", context);
  assert.equal(link.download, "PMR_Quality_CPK_Summary_202610.xlsx");
  assert.equal(link.clicked, true);
});

test("reports a rejected download without saving a workbook", async () => {
  const { link, context } = exportContext(async () => { throw new Error("Unauthorized"); });
  await runInNewContext(handler + ";exportCpkMaster()", context);
  assert.equal(context.err.value, "download error");
  assert.equal(context.exporting.value, false);
  assert.equal(link.clicked, undefined);
});
