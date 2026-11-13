import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [source, output, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');

tasks.getRange('A292:K292').copyFrom(tasks.getRange('A291:K291'), 'all');
project.getRange('A321:AK324').copyFrom(project.getRange('A317:AK320'), 'all');

tasks.getRange('A292:K292').values = [[
  'P0', 'SPC', 'SPC-PPC-AUDIT-TX-1113-TASK-001', 'PPC公式儲存沿用稽核交易',
  '測試站已發布／待登入重新儲存驗收', '是', 46339,
  'PPC更新已有BusinessDataAuditFilter交易時沿用外層交易，公式、版本與稽核事件維持同成同敗。',
  'SQLite回歸1/1、相關19/19與API build通過；測試API0.1.128、Web0.1.129，備份、權限撤銷、首頁／JS／CSS及同源health/version通過。',
  'D:/SPC/specs/20261113-ppc-audit-transaction-reuse/verification.md',
  '先前失敗請求已回滾，需登入後重新按儲存確認；完整API測試另有10項既有藥液歷史判定／修復失敗。正式未發布。'
]];

project.getRange('A321').values = [['2026-11-13：PPC公式儲存交易衝突修正']];
project.getRange('A322:AK322').values = [['系統',null,'工作項目',null,null,null,null,null,null,null,null,null,'實際日期',null,null,null,null,null,'狀態',null,null,null,null,null,null,null,null,'11/13 測試里程碑',null,null,null,null,null,null,null,null,null]];
project.getRange('A323:AK323').values = [['SPC',null,'◆ PPC公式儲存交易修正',null,null,null,null,null,null,null,null,null,46339,null,null,null,null,null,'測試站已發布／待登入重新儲存驗收',null,null,null,null,null,null,null,null,'◆ API128／相關19',null,null,null,null,null,null,null,null,null]];
project.getRange('A324').values = [['PPC更新沿用稽核外層交易，避免公式儲存因巢狀交易回傳500；公式版本與稽核事件維持原子性。測試API0.1.128，備份與連線驗證通過。先前失敗公式需重新按儲存；正式未發布。']];

workbook.recalculate();
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A291:K292',include:'values,formulas',tableMaxRows:3,tableMaxCols:11,maxChars:10000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:'專案進度表!A321:AK324',include:'values,formulas',tableMaxRows:5,tableMaxCols:37,maxChars:12000})).ndjson);
console.log((await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:3000})).ndjson);
const image = await workbook.render({sheetName:'專案進度表',range:'A321:AK324',scale:1.2,format:'png'});
await fs.writeFile(preview, new Uint8Array(await image.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
