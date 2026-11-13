import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [source, output, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');
const day = (Date.UTC(2026, 10, 13) - Date.UTC(1899, 11, 30)) / 86400000;

tasks.getRange('A287:K287').copyTo(tasks.getRange('A288:K288'), 'all');
tasks.getRange('A1').values = [['小工作總表（截至2026-11-13；藥液最終分析值顯示一致化）']];
tasks.getRange('B2').values = [[250]];
tasks.getRange('D2').values = [[37]];
tasks.getRange('A3').values = [['核心未完成37件，SPC相關13件；新增藥液最終分析值顯示一致化已發布Portal測試站，正式未發布。人工12件／待核銷4件另計。']];
tasks.getRange('A288:K288').values = [[
  'P0', 'Portal／SPC', 'SPC-CHEM-EFFECTIVE-VALUE-1113-TASK-001', '藥液新增／查詢／圖表最終分析值一致顯示',
  'Portal測試站已發布／正式未發布', '是', day,
  '入口保留初驗／複驗證據，明示最終（查詢／圖表）＝RecheckValue ?? MeasuredValue；複驗0仍有效。',
  'Portal UI 9/9、SPC圖表有效值焦點測試1/1、Portal API/Web與SPC API build通過；Portal測試API1.0.232／Web1.0.273，Web資產、Portal health、SPC health/version與實際SPC API基底通過。',
  'D:/SPC/specs/20261113-chemical-effective-value-entry-display/verification.md',
  '正式未發布；不改公式、F、schema或既有量測。完整既有歷史判定類別有2項既存失敗，已隔離記錄。'
]];
tasks.getRange('G288').setNumberFormat('yyyy-mm-dd');

project.getRange('A291:AK294').copyTo(project.getRange('A305:AK308'), 'all');
project.getRange('A305:AK308').clear({ applyTo: 'contents' });
project.getRange('A305').values = [['2026-11-13：藥液新增／查詢／圖表最終分析值一致顯示']];
project.getRange('A306').values = [['系統']];
project.getRange('C306').values = [['工作項目']];
project.getRange('M306').values = [['實際日期']];
project.getRange('S306').values = [['狀態']];
project.getRange('AB306').values = [['11/13 測試里程碑']];
project.getRange('A307').values = [['Portal／SPC']];
project.getRange('C307').values = [['◆ 最終（查詢／圖表）保存值明示']];
project.getRange('M307').values = [[day]];
project.getRange('M307').setNumberFormat('yyyy-mm-dd');
project.getRange('S307').values = [['Portal測試站已發布／正式未發布']];
project.getRange('AB307').values = [['◆ Portal API232／Web273']];
project.getRange('A308').values = [['入口初驗欄改為「初驗分析值」，同格明示最終（查詢／圖表）：有複驗取複驗、否則初驗；查詢與SPC圖表均使用同一已保存有效值。Portal UI 9/9、SPC圖表焦點測試1/1與build通過；測試Web／資產、Portal health、SPC health／version及實際SPC API基底通過。未改寫量測、公式、F或schema；正式未發布。']];

workbook.recalculate();
console.log((await workbook.inspect({ kind: 'table', range: '小工作總表!A1:K288', include: 'values,formulas', tableMaxRows: 8, tableMaxCols: 11, maxChars: 10000 })).ndjson);
console.log((await workbook.inspect({ kind: 'table', range: '專案進度表!A305:AK308', include: 'values,formulas', tableMaxRows: 4, tableMaxCols: 37, maxChars: 12000 })).ndjson);
console.log((await workbook.inspect({ kind: 'match', searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!', options: { useRegex: true, maxResults: 100 }, maxChars: 3000 })).ndjson);
const image = await workbook.render({ sheetName: '專案進度表', range: 'A305:AK308', scale: 1.2, format: 'png' });
await fs.writeFile(preview, new Uint8Array(await image.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
