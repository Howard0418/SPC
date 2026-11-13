import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [source, output, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');

tasks.getRange('A294:K294').copyFrom(tasks.getRange('A293:K293'), 'all');
project.getRange('A329:AK332').copyFrom(project.getRange('A325:AK328'), 'all');

tasks.getRange('A294:K294').values = [[
  'P0', 'SPC／Portal', 'SPC-CHEM-PREVIEW-EMPTY-SECONDARY-1113-TASK-001', '藥液空白第二滴定值預覽衝突修正',
  'SPC測試站已發布／待登入實際儲存驗收', '否', 46339,
  'Portal預覽以null、確認以空字串送出未提供的第二滴定值；SPC統一正規化為未提供值，避免快照雜湊誤判。',
  '紅燈測試重現409；修正後藥液／排程焦點回歸20/20與Release build通過。測試API0.1.130、Web0.1.129，備份、權限撤銷、資產MIME、Chrome health/version通過。',
  'D:/SPC/specs/20261113-chemical-preview-empty-secondary/verification.md',
  '程式與測試發布完成；待使用者重新整理Portal後以原資料實際儲存驗收，此為人工驗收註記，不重複計入核心未完成。正式未發布。'
]];

project.getRange('A329').values = [['2026-11-13：藥液空白第二滴定值預覽衝突修正']];
project.getRange('A330:AK330').values = [['系統',null,'工作項目',null,null,null,null,null,null,null,null,null,'實際日期',null,null,null,null,null,'狀態',null,null,null,null,null,null,null,null,'11/13 測試里程碑',null,null,null,null,null,null,null,null,null]];
project.getRange('A331:AK331').values = [['SPC／Portal',null,'◆ 空白第二滴定值正規化',null,null,null,null,null,null,null,null,null,46339,null,null,null,null,null,'SPC測試站已發布／待登入實際儲存驗收',null,null,null,null,null,null,null,null,'◆ API130／回歸20',null,null,null,null,null,null,null,null,null]];
project.getRange('A332').values = [['Portal預覽null、確認空字串造成快照雜湊不同；SPC已將空白第二滴定值統一為未提供值。紅燈重現後回歸20項與Release build通過，測試API0.1.130；待登入實際儲存驗收，正式未發布。']];

workbook.recalculate();
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A293:K294',include:'values,formulas',tableMaxRows:3,tableMaxCols:11,maxChars:12000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:'專案進度表!A329:AK332',include:'values,formulas',tableMaxRows:5,tableMaxCols:37,maxChars:12000})).ndjson);
console.log((await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:3000})).ndjson);
const image = await workbook.render({sheetName:'專案進度表',range:'A329:AK332',scale:1.2,format:'png'});
await fs.writeFile(preview, new Uint8Array(await image.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
