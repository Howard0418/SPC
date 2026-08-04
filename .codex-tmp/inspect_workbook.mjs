import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const inputPath = "D:/SPC/sample-data/批次轉換結果_藥液匯入檔.xlsx";
const input = await FileBlob.load(inputPath);
const workbook = await SpreadsheetFile.importXlsx(input);
const summary = await workbook.inspect({
  kind: "workbook,sheet,table,region",
  maxChars: 14000,
  tableMaxRows: 15,
  tableMaxCols: 15,
  tableMaxCellChars: 120,
});
console.log(summary.ndjson);
