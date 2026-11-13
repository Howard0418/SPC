import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [source, output, projectPreview, taskPreview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');
const taskValues = tasks.getUsedRange(true).values;
if (Number(taskValues[1][1]) !== 257 || Number(taskValues[1][3]) !== 43 || Number(taskValues[1][5]) !== 214) throw new Error('Unexpected current task summary');
const lastTaskRow = taskValues.reduce((last, row, index) => row[2] ? index + 1 : last, 1);
const taskRow = lastTaskRow + 1;
tasks.getRange(`A${taskRow}:K${taskRow}`).copyFrom(tasks.getRange(`A${lastTaskRow}:K${lastTaskRow}`), 'all');
tasks.getRange(`A${taskRow}:K${taskRow}`).values = [[
  'P0', 'SPC', 'SPC-CHEM-C1-EXCEL-ALIGN-1113-TASK-001', 'C1藥液計算與Excel三位精度對齊',
  '測試資料修正完成／正式未異動', '否', 46339,
  'C1六項公式保存精度2改3；鍍銅1氯離子保存真實滴定值0.198；鍍銅1硫酸調整目標由185改回Excel的180。',
  '11/12六筆已建立新公式／排程／Receipt／Trace v2；結果172.211、53.125、30.096、152.807、122.500、33.440。舊證據保持。',
  'D:/SPC/specs/20261113-c1-excel-calculation-alignment/verification.md',
  '5項核心測試、COPY_ONLY／CHECKSUM／VERIFYONLY、六組Preview／HistoryQuery、保護指紋與重跑pending=0通過；API0.1.130 test健康，網站不需發布。'
]];
tasks.getRange('A1').values = [['小工作總表（截至2026-11-13；C1藥液計算與Excel三位精度對齊）']];
tasks.getRange('B2').values = [[258]];
tasks.getRange('D2').values = [[43]];
tasks.getRange('F2').values = [[215]];
tasks.getRange('A3').values = [['核心未完成43件，SPC相關19件；C1藥液計算與Excel三位精度對齊新增後同次完成，不增加未完。人工12件／待核銷4件另計。']];

const projectValues = project.getUsedRange(true).values;
const lastProjectRow = projectValues.reduce((last, row, index) => row.some(value => value !== null && value !== '') ? index + 1 : last, 1);
const projectStart = lastProjectRow + 1;
project.getRange(`A${projectStart}:AK${projectStart + 3}`).copyFrom(project.getRange(`A${lastProjectRow - 3}:AK${lastProjectRow}`), 'all');
project.getRange(`A${projectStart}`).values = [['2026-11-13：C1藥液計算與Excel三位精度對齊']];
project.getRange(`A${projectStart + 1}:AK${projectStart + 1}`).values = [['系統',null,'工作項目',null,null,null,null,null,null,null,null,null,'實際日期',null,null,null,null,null,'狀態',null,null,null,null,null,null,null,null,'11/13 測試資料里程碑',null,null,null,null,null,null,null,null,null]];
project.getRange(`A${projectStart + 2}:AK${projectStart + 2}`).values = [['SPC',null,'◆ C1藥液計算與Excel對齊',null,null,null,null,null,null,null,null,null,46339,null,null,null,null,null,'測試資料修正完成／正式未異動',null,null,null,null,null,null,null,null,'◆ 六筆三位一致／Trace v2',null,null,null,null,null,null,null,null,null]];
project.getRange(`A${projectStart + 3}`).values = [['六項精度改3，氯離子真實滴定值0.198，鍍銅1硫酸調整目標180；六筆與Excel一致。備份、5項測試、舊證據、保護指紋及冪等通過。網站不需發布，正式未異動。']];
const milestone = project.shapes.add({
  geometry: 'diamond',
  anchor: { from: { row: projectStart + 1, col: 27, rowOffsetPx: 3, colOffsetPx: 2 }, extent: { widthPx: 8, heightPx: 8 } },
  fill: '#10B981', line: { style: 'solid', fill: '#047857', width: 1 }
});
milestone.name = 'SPC-CHEM-C1-EXCEL-ALIGN-1113-TASK-001 2026-11-13 測試資料完成';

workbook.recalculate();
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A1:K3',include:'values,formulas',tableMaxRows:3,tableMaxCols:11,maxChars:10000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:`小工作總表!A${taskRow}:K${taskRow}`,include:'values,formulas',tableMaxRows:2,tableMaxCols:11,maxChars:9000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:`專案進度表!A${projectStart}:AK${projectStart + 3}`,include:'values,formulas',tableMaxRows:5,tableMaxCols:37,maxChars:12000})).ndjson);
console.log((await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:3000})).ndjson);
const projectImage = await workbook.render({sheetName:'專案進度表',range:`A${projectStart}:AK${projectStart + 3}`,scale:1.2,format:'png'});
const taskImage = await workbook.render({sheetName:'小工作總表',range:`A${Math.max(1, taskRow - 2)}:K${taskRow}`,scale:1.2,format:'png'});
await fs.writeFile(projectPreview, new Uint8Array(await projectImage.arrayBuffer()));
await fs.writeFile(taskPreview, new Uint8Array(await taskImage.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
console.log(JSON.stringify({taskRow, projectStart}));
