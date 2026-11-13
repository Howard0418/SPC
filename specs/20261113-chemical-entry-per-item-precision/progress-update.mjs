import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [source, output, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');
const day = (Date.UTC(2026, 10, 13) - Date.UTC(1899, 11, 30)) / 86400000;

tasks.getRange('A288:K288').copyTo(tasks.getRange('A289:K289'), 'all');
tasks.getRange('A1').values = [['小工作總表（截至2026-11-13；藥液項目3／4位精度）']];
tasks.getRange('B2').values = [[251]];
tasks.getRange('D2').values = [[38]];
tasks.getRange('A3').values = [['核心未完成38件，SPC相關14件；新增藥液項目3／4位精度已發布Portal／SPC測試站，正式未發布。人工12件／待核銷4件另計。']];
tasks.getRange('A289:K289').values = [[
  'P0', 'Portal／SPC', 'SPC-CHEM-PRECISION-1113-TASK-001', '藥液量測依項目設定第3／4位精度',
  'Portal／SPC測試站已發布／正式未發布', '是', day,
  '公式設定的 decimalPlaces 套用滴定值、初驗／複驗分析值、最終值與單／批次保存；不截斷第4位。',
  'SPC xUnit焦點1/1、Portal UI10/10與兩端build通過；SPC測試backend、Portal API1.0.234／Web1.0.275已發布；Portal登入／資產、Portal health、SPC health/version與實際API基底通過。',
  'D:/SPC/specs/20261113-chemical-entry-per-item-precision/verification.md',
  '正式未發布；不改schema、公式、F或既有歷史量測。'
]];
tasks.getRange('G289').setNumberFormat('yyyy-mm-dd');

project.getRange('A305:AK308').copyTo(project.getRange('A309:AK312'), 'all');
project.getRange('A309:AK312').clear({ applyTo: 'contents' });
project.getRange('A309').values = [['2026-11-13：藥液量測依項目設定第3／4位精度']];
project.getRange('A310').values = [['系統']];
project.getRange('C310').values = [['工作項目']];
project.getRange('M310').values = [['實際日期']];
project.getRange('S310').values = [['狀態']];
project.getRange('AB310').values = [['11/13 測試里程碑']];
project.getRange('A311').values = [['Portal／SPC']];
project.getRange('C311').values = [['◆ 藥液公式小數位同步']];
project.getRange('M311').values = [[day]];
project.getRange('M311').setNumberFormat('yyyy-mm-dd');
project.getRange('S311').values = [['Portal／SPC測試站已發布／正式未發布']];
project.getRange('AB311').values = [['◆ Portal API234／Web275']];
project.getRange('A312').values = [['SPC預覽回傳有效公式的小數位；Portal滴定值、初驗／複驗分析值、最終值與單／批次保存依項目設定3或4位顯示。xUnit焦點1/1、Portal UI10/10與build通過；登入／資產、Portal health、SPC health／version與實際API基底通過。不改寫歷史資料、公式、F或schema；正式未發布。']];

workbook.recalculate();
console.log((await workbook.inspect({ kind: 'table', range: '小工作總表!A1:K289', include: 'values,formulas', tableMaxRows: 8, tableMaxCols: 11, maxChars: 10000 })).ndjson);
console.log((await workbook.inspect({ kind: 'table', range: '專案進度表!A309:AK312', include: 'values,formulas', tableMaxRows: 4, tableMaxCols: 37, maxChars: 12000 })).ndjson);
console.log((await workbook.inspect({ kind: 'match', searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!', options: { useRegex: true, maxResults: 100 }, maxChars: 3000 })).ndjson);
const image = await workbook.render({ sheetName: '專案進度表', range: 'A309:AK312', scale: 1.2, format: 'png' });
await fs.writeFile(preview, new Uint8Array(await image.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
