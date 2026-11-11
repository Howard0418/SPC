import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const [source, output, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');

tasks.getRange('E287:K287').values = [[
  'Portal正式已發布／SPC備份已授權但VERIFYONLY受阻', '是', 46337,
  '查詢頁採SPC effectiveValue；SPC測試發布principal已具PMR_SPC_TEST備份權限，但還原驗證需要更高且未授權的權限。',
  'SpcAuditApiTest003已加入測試庫db_backupoperator且BACKUP DATABASE=1；R2 Plan與COPY_ONLY備份成功。RESTORE VERIFYONLY回SQL262/class14，newFiles=0，API仍0.1.125。',
  'D:/SPC/specs/20261111-test-release-backup-permission/spec.md',
  '未授予dbcreator、CREATE ANY DATABASE或sysadmin；未套用API126，正式SPC未發布。待核准暫時較高權限或專用維護principal。'
]];
tasks.getRange('G287').setNumberFormat('yyyy-mm-dd');

project.getRange('A295:AK298').copyTo(project.getRange('A299:AK302'), 'all');
project.getRange('A299').values = [['2026-11-11：SPC測試發布備份權限']];
project.getRange('A300').values = [['系統']];
project.getRange('C300').values = [['工作項目']];
project.getRange('M300').values = [['實際日期']];
project.getRange('S300').values = [['狀態']];
project.getRange('AA300').values = [['11/11 權限里程碑']];
project.getRange('A301').values = [['SPC']];
project.getRange('C301').values = [['◆ 測試備份權限已授予／VERIFYONLY受阻']];
project.getRange('M301').values = [[46337]];
project.getRange('M301').setNumberFormat('yyyy-mm-dd');
project.getRange('S301').values = [['備份成功／還原驗證權限待核准']];
project.getRange('AA301').values = [['◆ DB備份權限1；SQL262阻擋']];
project.getRange('A302').values = [['測試發布principal已加入PMR_SPC_TEST db_backupoperator，COPY_ONLY備份成功；RESTORE VERIFYONLY需更高權限而回SQL262。未擴權、未套用API126、未發布正式SPC。']];
const milestone = project.shapes.add({
  geometry: 'diamond',
  anchor: { from: { row: 300, col: 27, rowOffsetPx: 3, colOffsetPx: 2 }, extent: { widthPx: 8, heightPx: 8 } },
  fill: '#F59E0B',
  line: { style: 'solid', fill: '#B45309', width: 1 }
});
milestone.name = 'SPC-RELEASE-BACKUP-PERMISSION-1111-TASK-001 2026-11-11 VERIFYONLY受阻';

workbook.recalculate();
const errors = (await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:3000})).ndjson;
if (!errors.includes('matched 0')) throw new Error('formula errors');
const image = await workbook.render({sheetName:'專案進度表',range:'A299:AK302',scale:1.2,format:'png'});
await fs.writeFile(preview, new Uint8Array(await image.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A287:K287',include:'values,formulas',tableMaxRows:2,tableMaxCols:11,maxChars:8000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:'專案進度表!A299:AK302',include:'values,formulas',tableMaxRows:6,tableMaxCols:40,maxChars:12000})).ndjson);
console.log((await workbook.inspect({kind:'drawing',sheetId:'專案進度表',maxChars:100000})).ndjson.includes(milestone.name)?'milestone=ok':'milestone=missing');
