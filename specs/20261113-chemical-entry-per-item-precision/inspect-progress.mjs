import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(process.argv[2]));
for (const range of ['小工作總表!A287:K290', '專案進度表!A305:AK312']) {
  console.log((await workbook.inspect({ kind: 'table', range, include: 'values,formulas', tableMaxRows: 16, tableMaxCols: 40, maxChars: 18000 })).ndjson);
}
