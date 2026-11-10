import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [source, output] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const project = workbook.worksheets.getItem('專案進度表');
const tasks = workbook.worksheets.getItem('小工作總表');
const excelDay = (Date.UTC(2026, 10, 10) - Date.UTC(1899, 11, 30)) / 86400000;

tasks.getRange('A274:K274').copyTo(tasks.getRange('A275:K275'), 'all');
tasks.getRange('A275:K275').values = [[
  'P0','SPC／Portal','SPC-CHEM-DAILY-SCOPE-1110-TASK-001','藥液日報與咬蝕量管制範圍隔離',
  '正式發布完成／待正式畫面確認','否',excelDay,
  '藥液日報、月曆與PortalDaily防重只處理啟用中的CHEM；PROCESS咬蝕子組不再造成藥液重複409，真正CHEM重複仍阻擋。',
  '使用者已驗收測試正常。後端92通過／1略過、聚焦3項及Portal契約1項通過；測試API0.1.117／Portal1.0.267，正式API0.1.104／Portal1.0.249。正式Web／資產／health／version／CORS通過，COPY_ONLY／CHECKSUM備份完成；正式登入畫面請使用者確認。',
  'D:/SPC/specs/20261110-chemical-daily-scope-isolation/verification.md',
  '11/10使用者回報QE2正式日報誤判重複，測試驗收正常後明確授權正式發布。'
]];
tasks.getRange('G275').setNumberFormat('yyyy-mm-dd');
tasks.getRange('A275:K275').format.rowHeight = 82;
tasks.getRange('A1').values = [['小工作總表（截至2026-11-10；藥液日報CHEM範圍隔離正式發布）']];
tasks.getRange('B2').values = [[242]];
tasks.getRange('D2').values = [[34]];
tasks.getRange('F2').values = [[206]];
tasks.getRange('A3').values = [['核心未完成34件，SPC相關10件；藥液日報CHEM範圍隔離已完成測試驗收並發布正式API0.1.104／Portal1.0.249。正式資料、公式、F表及schema未修改；正式登入畫面待使用者確認。']];

project.getRange('A239:AK242').copyTo(project.getRange('A243:AK246'), 'all');
project.getRange('A243').values = [['2026-11-10：藥液日報CHEM範圍隔離（正式發布完成）']];
project.getRange('A244').values = [['系統']]; project.getRange('C244').values = [['工作項目']];
project.getRange('M244').values = [['實際日期']]; project.getRange('S244').values = [['狀態']]; project.getRange('AA244').values = [['11/10 正式里程碑']];
project.getRange('A245').values = [['SPC／Portal']]; project.getRange('C245').values = [['◆ 藥液日報與咬蝕量範圍隔離']];
project.getRange('M245').values = [[excelDay]]; project.getRange('M245').setNumberFormat('yyyy-mm-dd');
project.getRange('S245').values = [['正式發布完成／待正式畫面確認']]; project.getRange('AA245').values = [['◆ API0.1.104／Portal1.0.249']];
project.getRange('A246').values = [['QE2同日多筆PROCESS咬蝕資料不再觸發藥液重複；真正CHEM重複仍回409。測試人工驗收正常；正式Web、資產、health/version、CORS及備份通過，資料／公式／F／schema保持。']];
project.getRange('AI5').values = [[excelDay]]; project.getRange('AI5').setNumberFormat('yyyy-mm-dd');
project.getRange('AJ5').values = [['11/10藥液日報CHEM範圍隔離正式完成；既有10項SPC未完成工作保持']];
project.getRange('AK5').values = [['正式API0.1.104／Portal1.0.249；測試驗收正常，正式Web／資產／API基底驗證通過，正式資料與schema未異動。']];
const milestone=project.shapes.add({geometry:'diamond',anchor:{from:{row:244,col:27,rowOffsetPx:3,colOffsetPx:2},extent:{widthPx:8,heightPx:8}},fill:'#22C55E',line:{style:'solid',fill:'#15803D',width:1}});
milestone.name='SPC-CHEM-DAILY-SCOPE-1110-TASK-001 2026-11-10 正式發布完成';

workbook.recalculate();
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A1:K3',include:'values,formulas',tableMaxRows:3,tableMaxCols:11,maxChars:7000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A275:K275',include:'values,formulas',tableMaxRows:1,tableMaxCols:11,maxChars:5000})).ndjson);
console.log((await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:3000})).ndjson);
const preview=await workbook.render({sheetName:'專案進度表',range:'A239:AK246',scale:1.2,format:'png'});
await fs.writeFile(new URL('progress-after.png',import.meta.url),new Uint8Array(await preview.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
