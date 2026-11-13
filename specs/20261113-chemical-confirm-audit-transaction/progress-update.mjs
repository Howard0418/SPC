import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [source, output, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');

tasks.getRange('A293:K293').copyFrom(tasks.getRange('A292:K292'), 'all');
project.getRange('A325:AK328').copyFrom(project.getRange('A321:AK324'), 'all');

tasks.getRange('A293:K293').values = [[
  'P0', 'SPC／Portal', 'SPC-CHEM-CONFIRM-AUDIT-TX-1113-TASK-001', '藥液量測確認與稽核交易相容',
  'SPC測試站已發布／待登入實際儲存驗收', '是', 46339,
  '藥液Confirm由服務管理完整交易，稽核事件加入同一交易；量測、Receipt、Trace、統計與稽核同成同敗，通知只在提交後執行。',
  '整合回歸先重現503；成功與稽核失敗回滾均通過。相關13/13及API build通過；測試API0.1.129、Web0.1.129，備份、權限撤銷、資產MIME與同源health/version通過。',
  'D:/SPC/specs/20261113-chemical-confirm-audit-transaction/verification.md',
  '未代替使用者寫入真實量測；請登入後重新整理並重試儲存，再查詢回讀確認。正式未發布。'
]];

project.getRange('A325').values = [['2026-11-13：藥液量測確認交易衝突修正']];
project.getRange('A326:AK326').values = [['系統',null,'工作項目',null,null,null,null,null,null,null,null,null,'實際日期',null,null,null,null,null,'狀態',null,null,null,null,null,null,null,null,'11/13 測試里程碑',null,null,null,null,null,null,null,null,null]];
project.getRange('A327:AK327').values = [['SPC／Portal',null,'◆ 藥液Confirm交易相容',null,null,null,null,null,null,null,null,null,46339,null,null,null,null,null,'SPC測試站已發布／待登入實際儲存驗收',null,null,null,null,null,null,null,null,'◆ API129／相關13',null,null,null,null,null,null,null,null,null]];
project.getRange('A328').values = [['藥液Confirm保留服務完整交易，稽核不再建立衝突外層交易；量測、Receipt、Trace、統計與稽核同成同敗，通知只在提交後執行。相關13項與build通過，測試API0.1.129。待登入重試儲存；正式未發布。']];

workbook.recalculate();
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A292:K293',include:'values,formulas',tableMaxRows:3,tableMaxCols:11,maxChars:10000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:'專案進度表!A325:AK328',include:'values,formulas',tableMaxRows:5,tableMaxCols:37,maxChars:12000})).ndjson);
console.log((await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:3000})).ndjson);
const image = await workbook.render({sheetName:'專案進度表',range:'A325:AK328',scale:1.2,format:'png'});
await fs.writeFile(preview, new Uint8Array(await image.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
