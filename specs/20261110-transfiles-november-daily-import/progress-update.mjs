import fs from 'node:fs/promises';
import {FileBlob,SpreadsheetFile} from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const [source,output]=process.argv.slice(2);
const workbook=await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks=workbook.worksheets.getItem('小工作總表');
const project=workbook.worksheets.getItem('專案進度表');
const excelDay=(Date.UTC(2026,10,10)-Date.UTC(1899,11,30))/86400000;

tasks.getRange('A275:K275').copyTo(tasks.getRange('A276:K276'),'all');
tasks.getRange('A276:K276').values=[[
  'P0','TransFiles／SPC','TRANSFILES-NOV-DAILY-1110-TASK-001','11/01～11/10藥液／咬蝕來源路徑修正與唯讀清冊',
  '唯讀預檢完成／待測試庫防重對帳','否',excelDay,
  '修正藥液已失效UNC路徑；補液位／更槽檔排除，中班及N1/N2開收線納入獨立業務鍵。',
  '62項回歸通過。藥液8檔368列（早班365／中班3，開38／收31／一般299），來源誤判重複0；咬蝕21份1,750點可預覽，11/10 QE1缺S4/S14隔離。未呼叫API或寫入DB。',
  'D:/SPC/specs/20261110-transfiles-november-daily-import/verification.md',
  '11/10使用者要求建立11月補匯與每日09:00前一日工具；本件只完成第一階段唯讀預檢。'
]];
tasks.getRange('G276').setNumberFormat('yyyy-mm-dd');
tasks.getRange('A276:K276').format.rowHeight=82;
tasks.getRange('A1').values=[['小工作總表（截至2026-11-10；11月藥液／咬蝕唯讀預檢）']];
tasks.getRange('B2').values=[[243]]; tasks.getRange('D2').values=[[34]]; tasks.getRange('F2').values=[[207]];
tasks.getRange('A3').values=[['核心未完成34件，SPC相關10件；11/01～11/10藥液／咬蝕來源唯讀預檢完成。後續測試庫防重對帳、正式補匯及09:00排程仍沿原TransFiles未完工作管理，不重複計數。']];

project.getRange('A243:AK246').copyTo(project.getRange('A247:AK250'),'all');
project.getRange('A247').values=[['2026-11-10：11月藥液／咬蝕來源唯讀預檢']];
project.getRange('A248').values=[['系統']]; project.getRange('C248').values=[['工作項目']]; project.getRange('M248').values=[['實際日期']]; project.getRange('S248').values=[['狀態']]; project.getRange('AA248').values=[['11/10 預檢里程碑']];
project.getRange('A249').values=[['TransFiles／SPC']]; project.getRange('C249').values=[['◆ 11/01～11/10藥液／咬蝕來源預檢']]; project.getRange('M249').values=[[excelDay]]; project.getRange('M249').setNumberFormat('yyyy-mm-dd'); project.getRange('S249').values=[['唯讀預檢完成／待測試庫對帳']]; project.getRange('AA249').values=[['◆ 368列／21份／1隔離']];
project.getRange('A250').values=[['藥液路徑已修正，補液位／更槽排除，中班及N1/N2開收線分離；藥液368列、咬蝕21份1,750點可進入後續對帳，11/10 QE1缺S4/S14隔離。未寫入測試／正式DB，未建立排程。']];
project.getRange('AI5').values=[[excelDay]]; project.getRange('AI5').setNumberFormat('yyyy-mm-dd');
project.getRange('AJ5').values=[['11/10完成11月藥液／咬蝕唯讀預檢；後續測試庫對帳與排程待辦']];
project.getRange('AK5').values=[['62項回歸通過；藥液368列、咬蝕1,750點，QE1 11/10缺S4/S14隔離，DB寫入0。']];
const milestone=project.shapes.add({geometry:'diamond',anchor:{from:{row:248,col:27,rowOffsetPx:3,colOffsetPx:2},extent:{widthPx:8,heightPx:8}},fill:'#F59E0B',line:{style:'solid',fill:'#B45309',width:1}});
milestone.name='TRANSFILES-NOV-DAILY-1110-TASK-001 2026-11-10 唯讀預檢完成';
workbook.recalculate();
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A1:K3',include:'values,formulas',tableMaxRows:3,tableMaxCols:11,maxChars:7000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A276:K276',include:'values,formulas',tableMaxRows:1,tableMaxCols:11,maxChars:5000})).ndjson);
console.log((await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:3000})).ndjson);
const preview=await workbook.render({sheetName:'專案進度表',range:'A243:AK250',scale:1.2,format:'png'});
await fs.writeFile('D:/SPC/.november-daily-import-1110/progress-after.png',new Uint8Array(await preview.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
