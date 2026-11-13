import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [source, output, projectPreview, taskPreview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');
const taskRows = tasks.getUsedRange().values.length;
const projectRows = project.getUsedRange().values.length;
const taskRow = taskRows + 1;
const projectStart = projectRows + 1;

tasks.getRange(`A${taskRow}:K${taskRow}`).copyFrom(tasks.getRange(`A${taskRows}:K${taskRows}`), 'all');
project.getRange(`A${projectStart}:AK${projectStart + 3}`).copyFrom(project.getRange(`A${projectRows - 3}:AK${projectRows}`), 'all');

tasks.getRange(`A${taskRow}:K${taskRow}`).values = [[
  'P0', 'SPC', 'SPC-CHEM-DELETE-TRANSACTION-1113-TASK-001', '藥液量測刪除沿用稽核交易',
  '測試站完成／待實際刪除驗收／正式未發布', '是', 46339,
  '刪除端點沿用BusinessDataAuditFilter既有交易；無外層交易時仍自行建立交易。量測採邏輯刪除，保留不可變Receipt／Trace。',
  'SQLite紅燈先重現503；修正後刪除、稽核、手動量測與藥液確認相關36/36通過。測試API0.1.132、Web資產、同源與8901 health/version均通過。',
  'D:/SPC/specs/20261113-chemical-delete-audit-transaction/verification.md',
  '未代替使用者刪除量測；待登入測試站重試原刪除操作。無schema／公式／F／既有資料修補，正式未授權。'
]];

const registered = Number(tasks.getRange('B2').values[0][0] ?? 0) + 1;
const unfinished = Number(tasks.getRange('D2').values[0][0] ?? 0) + 1;
const completed = Number(tasks.getRange('F2').values[0][0] ?? 0);
tasks.getRange('A1').values = [['小工作總表（截至2026-11-13；藥液刪除交易修正已發布測試站待驗收）']];
tasks.getRange('B2').values = [[registered]];
tasks.getRange('D2').values = [[unfinished]];
tasks.getRange('F2').values = [[completed]];
tasks.getRange('A3').values = [[`核心未完成${unfinished}件；藥液刪除交易修正已完成測試站，待使用者實際刪除驗收後核銷。人工驗收與待核銷依原表另計，其他專案狀態不變。`]];

project.getRange(`A${projectStart}`).values = [['2026-11-13：藥液量測刪除沿用稽核交易']];
project.getRange(`A${projectStart + 1}:AK${projectStart + 1}`).values = [['系統',null,'工作項目',null,null,null,null,null,null,null,null,null,'實際日期',null,null,null,null,null,'狀態',null,null,null,null,null,null,null,null,'11/13 測試站里程碑',null,null,null,null,null,null,null,null,null]];
project.getRange(`A${projectStart + 2}:AK${projectStart + 2}`).values = [['SPC',null,'◆ 藥液量測刪除交易修正',null,null,null,null,null,null,null,null,null,46339,null,null,null,null,null,'測試站完成／待實際刪除驗收',null,null,null,null,null,null,null,null,'◆ API 0.1.132／36項通過',null,null,null,null,null,null,null,null,null]];
project.getRange(`A${projectStart + 3}`).values = [['刪除沿用外層業務稽核交易，無外層時維持自行交易；量測邏輯刪除並保留Receipt／Trace。測試API已發布，正式未發布。']];
const milestone = project.shapes.add({
  geometry: 'diamond',
  anchor: { from: { row: projectStart + 1, col: 27, rowOffsetPx: 3, colOffsetPx: 2 }, extent: { widthPx: 8, heightPx: 8 } },
  fill: '#F59E0B', line: { style: 'solid', fill: '#B45309', width: 1 }
});
milestone.name = 'SPC-CHEM-DELETE-TRANSACTION-1113-TASK-001 2026-11-13 測試站完成待驗收';

workbook.recalculate();
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A1:K3',include:'values,formulas',tableMaxRows:3,tableMaxCols:11,maxChars:10000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:`小工作總表!A${taskRow}:K${taskRow}`,include:'values,formulas',tableMaxRows:2,tableMaxCols:11,maxChars:10000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:`專案進度表!A${projectStart}:AK${projectStart + 3}`,include:'values,formulas',tableMaxRows:5,tableMaxCols:37,maxChars:15000})).ndjson);
console.log((await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:300},maxChars:5000})).ndjson);
const projectImage = await workbook.render({sheetName:'專案進度表',range:`A${projectStart}:AK${projectStart + 3}`,scale:1.2,format:'png'});
const taskImage = await workbook.render({sheetName:'小工作總表',range:`A${Math.max(1, taskRow - 2)}:K${taskRow}`,scale:1.2,format:'png'});
await fs.writeFile(projectPreview, new Uint8Array(await projectImage.arrayBuffer()));
await fs.writeFile(taskPreview, new Uint8Array(await taskImage.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
