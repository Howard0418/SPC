import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [source, output] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const project = workbook.worksheets.getItem('專案進度表');
const tasks = workbook.worksheets.getItem('小工作總表');
const excelDay = (Date.UTC(2026, 10, 10) - Date.UTC(1899, 11, 30)) / 86400000;

// TASK-009 remains open: performance acceptance is complete, but the signed-in UI capture is externally blocked.
tasks.getRange('E262').values = [['部分完成／登入畫面待驗']];
tasks.getRange('F262').values = [['是']];
tasks.getRange('G262').values = [[excelDay]];
tasks.getRange('G262').setNumberFormat('yyyy-mm-dd');
tasks.getRange('H262').values = [['11/10：同機雙程序、同一PMR_SPC_TEST、交替請求完成3輪基準；讀取新增-0.170～0.384ms，關鍵寫入新增5.316～12.729ms，均通過短基準絕對門檻。測試API0.1.116已發布。']];
tasks.getRange('I262').values = [['35項焦點、113項稽核回歸（3項SQL隔離依環境明示略過）及Release build通過；實際測試API新增／刪除Part與稽核事件一致、暫存0。內建與Windows瀏覽器服務皆不可用，登入後查詢／匯出畫面與遮罩截圖仍待驗；正式未發布。']];
tasks.getRange('A262:K262').format.rowHeight = 96;
tasks.getRange('A1').values = [['小工作總表（截至2026-11-10；稽核效能完成、登入畫面待驗）']];
tasks.getRange('A3').values = [['核心未完成41件，SPC相關11件；稽核TASK-009效能與測試API0.1.116驗證完成，登入後畫面／遮罩截圖仍待驗，故本件不核銷；正式未發布。']];

// Append one native four-row milestone block without changing the incomplete counts.
project.getRange('A231:AK234').copyTo(project.getRange('A235:AK238'), 'all');
project.getRange('A235').values = [['2026-11-10：SPC稽核整案效能驗收（效能完成／登入畫面待驗）']];
project.getRange('A236').values = [['系統']];
project.getRange('C236').values = [['工作項目']];
project.getRange('M236').values = [['實際日期']];
project.getRange('S236').values = [['狀態']];
project.getRange('AA236').values = [['11/10 測試里程碑']];
project.getRange('A237').values = [['SPC']];
project.getRange('C237').values = [['◆ 稽核效能基準與原子寫入最佳化']];
project.getRange('M237').values = [[excelDay]];
project.getRange('M237').setNumberFormat('yyyy-mm-dd');
project.getRange('S237').values = [['效能完成／登入畫面待驗']];
project.getRange('AA237').values = [['◆ API0.1.116效能通過']];
project.getRange('A238').values = [['同機同資料庫3輪基準均通過；35項焦點、113項稽核回歸及Release build通過，實站新增／刪除與稽核原子性驗證完成。登入後畫面與遮罩截圖因兩套瀏覽器服務不可用仍待驗；TASK-009保持未完成，正式未發布。']];
project.getRange('AI5').values = [[excelDay]];
project.getRange('AI5').setNumberFormat('yyyy-mm-dd');
project.getRange('AJ5').values = [['11/10稽核效能與測試API0.1.116完成；登入畫面待驗']];
project.getRange('AK5').values = [['TASK-009效能3輪通過、測試站API0.1.116與高風險新增／刪除稽核驗證完成；瀏覽器服務不可用，登入後查詢／匯出畫面及遮罩截圖仍待驗，正式未發布。']];

const milestone = project.shapes.add({
  geometry: 'diamond',
  anchor: { from: { row: 236, col: 27, rowOffsetPx: 3, colOffsetPx: 2 }, extent: { widthPx: 8, heightPx: 8 } },
  fill: '#F59E0B',
  line: { style: 'solid', fill: '#B45309', width: 1 }
});
milestone.name = 'SPC-AUDIT-1103-TASK-009 2026-11-10 效能完成／畫面待驗';

workbook.recalculate();
for (const range of ['專案進度表!A231:AK238', '小工作總表!A262:K262']) {
  console.log((await workbook.inspect({ kind: 'table', range, include: 'values,formulas', tableMaxRows: 10, tableMaxCols: 40, maxChars: 9000 })).ndjson);
}
console.log((await workbook.inspect({ kind: 'match', searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!', options: { useRegex: true, maxResults: 100 }, maxChars: 3000 })).ndjson);

await fs.mkdir('D:/SPC/.codex-progress-work/audit009-performance', { recursive: true });
const progressImage = await workbook.render({ sheetName: '專案進度表', range: 'A231:AK238', scale: 1.2, format: 'png' });
await fs.writeFile('D:/SPC/.codex-progress-work/audit009-performance/project.png', new Uint8Array(await progressImage.arrayBuffer()));
const tasksImage = await workbook.render({ sheetName: '小工作總表', range: 'A260:K263', scale: 1.2, format: 'png' });
await fs.writeFile('D:/SPC/.codex-progress-work/audit009-performance/tasks.png', new Uint8Array(await tasksImage.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
