import assert from "node:assert/strict";
import test from "node:test";
import { describeAuditCode, describeAuditTarget } from "../src/utils/auditEventLabels.js";

const t = key => ({
  "spc.audit.labels.pages.auditEvents": "使用者活動紀錄",
  "spc.audit.labels.events.pageEnter": "進入頁面",
  "spc.audit.labels.outcomes.succeeded": "成功",
  "spc.audit.labels.sources.webRouter": "網頁導覽",
  "spc.audit.labels.targets.operator": "使用者"
}[key] || key);

test("已知稽核代碼顯示可讀名稱並保留原始代碼", () => {
  assert.deepEqual(describeAuditCode("pages", "admin.audit.events", t), { label: "使用者活動紀錄", code: "admin.audit.events" });
  assert.deepEqual(describeAuditCode("events", "navigation.page.enter", t), { label: "進入頁面", code: "navigation.page.enter" });
  assert.deepEqual(describeAuditCode("outcomes", "Succeeded", t), { label: "成功", code: "Succeeded" });
  assert.deepEqual(describeAuditCode("sources", "WebRouter", t), { label: "網頁導覽", code: "WebRouter" });
});

test("未知與空白代碼不誤譯或重複", () => {
  assert.deepEqual(describeAuditCode("pages", "future.page", t), { label: "future.page", code: "" });
  assert.deepEqual(describeAuditCode("events", "", t), { label: "-", code: "" });
  assert.deepEqual(describeAuditCode("events", null, t), { label: "-", code: "" });
});

test("目標類型可讀但保留類型與識別碼", () => {
  assert.deepEqual(describeAuditTarget("Operator", 27, t), { label: "使用者 / 27", code: "Operator" });
  assert.deepEqual(describeAuditTarget("UnknownType", "A-1", t), { label: "UnknownType / A-1", code: "" });
  assert.deepEqual(describeAuditTarget(null, null, t), { label: "-", code: "" });
});
