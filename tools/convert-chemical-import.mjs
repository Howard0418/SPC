import fs from "node:fs";
import path from "node:path";
import * as xlsx from "../frontend/mes-spc-web/node_modules/xlsx/xlsx.mjs";

const [, , inputArg, outputArg] = process.argv;

if (!inputArg || !outputArg) {
  console.error("Usage: node tools/convert-chemical-import.mjs <input.xlsx> <output.xlsx>");
  process.exit(2);
}

const inputPath = path.resolve(inputArg);
const outputPath = path.resolve(outputArg);

function normalizeText(value) {
  return String(value ?? "")
    .replace(/\r?\n/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

function normalizeDate(value) {
  const raw = normalizeText(value);
  if (/^\d{8}$/.test(raw)) {
    return `${raw.slice(0, 4)}-${raw.slice(4, 6)}-${raw.slice(6, 8)}`;
  }

  const parsed = new Date(raw);
  if (!Number.isNaN(parsed.getTime())) {
    const y = parsed.getFullYear();
    const m = String(parsed.getMonth() + 1).padStart(2, "0");
    const d = String(parsed.getDate()).padStart(2, "0");
    return `${y}-${m}-${d}`;
  }

  return raw;
}

function normalizeTime(value) {
  const raw = normalizeText(value);
  if (/^\d+(\.\d+)?$/.test(raw)) {
    const totalSeconds = Math.round(Number(raw) * 24 * 60 * 60);
    const h = String(Math.floor(totalSeconds / 3600) % 24).padStart(2, "0");
    const m = String(Math.floor((totalSeconds % 3600) / 60)).padStart(2, "0");
    const s = String(totalSeconds % 60).padStart(2, "0");
    return `${h}:${m}:${s}`;
  }
  return raw || "00:00:00";
}

function isNumeric(value) {
  return value !== null && value !== undefined && value !== "" && Number.isFinite(Number(value));
}

const workbook = xlsx.read(fs.readFileSync(inputPath), { cellDates: false, raw: false, type: "buffer" });
const firstSheetName = workbook.SheetNames[0];
const sourceSheet = workbook.Sheets[firstSheetName];
const rows = xlsx.utils.sheet_to_json(sourceSheet, { defval: "" });

const outputRows = [];
let skipped = 0;

for (const row of rows) {
  const measuredValue = normalizeText(row["量測值1"]);
  if (!isNumeric(measuredValue)) {
    skipped += 1;
    continue;
  }

  const measuredDate = normalizeDate(row["量測日期"]);
  const measuredTime = normalizeTime(row["量測時間"]);
  const sourceFile = normalizeText(row["來源檔案"]);
  const mesLot = normalizeText(row["MES-批號"]);

  outputRows.push({
    ControlScope: "CHEMICAL",
    ProcessCode: "MSAP",
    MachineCode: normalizeText(row["製程/線別"]),
    TankCode: normalizeText(row["機台/槽位"]),
    CharacteristicCode: normalizeText(row["管制項目"]),
    CharacteristicName: normalizeText(row["管制項目"]),
    MeasuredValue: Number(measuredValue),
    MeasuredAt: `${measuredDate} ${measuredTime}`,
    SampleNo: 1,
    LotNo: mesLot || sourceFile,
    Operator: normalizeText(row["量測員"]),
    SourceSheet: normalizeText(row["來源工作表"]),
    SourceFile: sourceFile,
  });
}

const outBook = xlsx.utils.book_new();
const outSheet = xlsx.utils.json_to_sheet(outputRows, {
  header: [
    "ControlScope",
    "ProcessCode",
    "MachineCode",
    "TankCode",
    "CharacteristicCode",
    "CharacteristicName",
    "MeasuredValue",
    "MeasuredAt",
    "SampleNo",
    "LotNo",
    "Operator",
    "SourceSheet",
    "SourceFile",
  ],
});

outSheet["!cols"] = [
  { wch: 14 },
  { wch: 12 },
  { wch: 12 },
  { wch: 28 },
  { wch: 22 },
  { wch: 22 },
  { wch: 14 },
  { wch: 22 },
  { wch: 10 },
  { wch: 26 },
  { wch: 14 },
  { wch: 18 },
  { wch: 26 },
];

xlsx.utils.book_append_sheet(outBook, outSheet, "計量型藥液匯入");
fs.mkdirSync(path.dirname(outputPath), { recursive: true });
const outputBuffer = xlsx.write(outBook, { bookType: "xlsx", type: "buffer" });
fs.writeFileSync(outputPath, outputBuffer);

console.log(JSON.stringify({
  input: inputPath,
  output: outputPath,
  sourceRows: rows.length,
  convertedRows: outputRows.length,
  skippedRows: skipped,
}, null, 2));
