import fs from "node:fs/promises";
import { FileBlob, SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const inputPath = "D:/SPC/sample-data/批次轉換結果_藥液匯入檔.xlsx";
const outputDir = "D:/SPC/outputs/019fc503-2821-7bd0-88f4-733b61f1098c";
const outputPath = `${outputDir}/藥液_線別槽位管制項目_中英文拆分.xlsx`;

function splitZhEn(value) {
  const text = value == null ? "" : String(value).trim();
  const zh = (text.match(/[\p{Script=Han}]+/gu) || []).join(" ").replace(/\s+/g, " ").trim();
  const en = text.replace(/[\p{Script=Han}]+/gu, " ").replace(/\s+/g, " ").trim();
  return [zh, en];
}

const input = await FileBlob.load(inputPath);
const source = await SpreadsheetFile.importXlsx(input);
const sourceSheet = source.worksheets.getItemAt(0);
const used = sourceSheet.getUsedRange(true);
const rowCount = used.rowCount;
const sourceRows = sourceSheet.getRange(`E2:G${rowCount}`).values;

const expandedRows = sourceRows.map(([line, slot, control]) => {
  const [slotZh, slotEn] = splitZhEn(slot);
  const [controlZh, controlEn] = splitZhEn(control);
  return [line ?? "", slotZh, slotEn, controlZh, controlEn];
});
const seen = new Set();
const outputRows = expandedRows.filter((row) => {
  const key = JSON.stringify(row);
  if (seen.has(key)) return false;
  seen.add(key);
  return true;
});

const workbook = Workbook.create();
const sheet = workbook.worksheets.add("整理結果");
sheet.showGridLines = false;
sheet.getRange("A1:E1").values = [["線別", "槽位（中文）", "槽位（英文）", "管制項目（中文）", "管制項目（英文）"]];
sheet.getRangeByIndexes(1, 0, outputRows.length, 5).values = outputRows;

const fullRange = sheet.getRange(`A1:E${outputRows.length + 1}`);
const header = sheet.getRange("A1:E1");
header.format = {
  fill: "#1F4E78",
  font: { bold: true, color: "#FFFFFF" },
  horizontalAlignment: "center",
  verticalAlignment: "center",
  borders: { preset: "outside", style: "thin", color: "#17365D" },
};
header.format.rowHeight = 24;
fullRange.format.font = { name: "Microsoft JhengHei", size: 10 };
sheet.getRange(`A2:A${outputRows.length + 1}`).format.horizontalAlignment = "center";
sheet.getRange(`A2:E${outputRows.length + 1}`).format.borders = {
  insideHorizontal: { style: "thin", color: "#E7EAF0" },
};
sheet.getRange("A:A").format.columnWidth = 11;
sheet.getRange("B:B").format.columnWidth = 20;
sheet.getRange("C:C").format.columnWidth = 28;
sheet.getRange("D:D").format.columnWidth = 22;
sheet.getRange("E:E").format.columnWidth = 24;
sheet.freezePanes.freezeRows(1);
sheet.tables.add(`A1:E${outputRows.length + 1}`, true, "ExtractedChemicalItems");

await fs.mkdir(outputDir, { recursive: true });
const preview = await workbook.render({ sheetName: "整理結果", range: "A1:E30", scale: 1.5, format: "png" });
await fs.writeFile(`${outputDir}/preview.png`, new Uint8Array(await preview.arrayBuffer()));

const check = await workbook.inspect({
  kind: "table",
  range: "整理結果!A1:E20",
  include: "values,formulas",
  tableMaxRows: 20,
  tableMaxCols: 5,
  maxChars: 8000,
});
console.log(check.ndjson);
const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A",
  options: { useRegex: true, maxResults: 50 },
  summary: "final formula error scan",
});
console.log(errors.ndjson);

const out = await SpreadsheetFile.exportXlsx(workbook);
await out.save(outputPath);
console.log(JSON.stringify({
  outputPath,
  sourceRows: sourceRows.length,
  outputRows: outputRows.length,
  duplicatesRemoved: expandedRows.length - outputRows.length,
}));
