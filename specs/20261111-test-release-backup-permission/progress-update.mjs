import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const [source, output, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');

tasks.getRange('E287:K287').values = [[
  'Portal正式已發布／SPC測試API126已完成', '是', 46337,
  '查詢頁採SPC effectiveValue；SPC測試發布已完成必要COPY_ONLY、CHECKSUM、RESTORE VERIFYONLY及連線驗證。',
  'R3完成，測試API0.1.126／test；Web、JS/CSS MIME、health、version與兩AppPool通過。暫時dbcreator已撤除，sysadmin始終未授予。',
  'D:/SPC/specs/20261111-test-release-backup-permission/spec.md',
  '最終dbcreator=0／sysadmin=0／BACKUP DATABASE=1；正式SPC未發布。2026/11/9 N1兩張截圖為不同槽位／項目，兩環境資料一致。'
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
project.getRange('C301').values = [['◆ 測試API126發布完成／暫時權限已撤除']];
project.getRange('M301').values = [[46337]];
project.getRange('M301').setNumberFormat('yyyy-mm-dd');
project.getRange('S301').values = [['測試發布完成／正式SPC未發布']];
project.getRange('AA301').values = [['◆ API126；dbcreator已撤除']];
project.getRange('A302').values = [['R3完成PMR_SPC_TEST COPY_ONLY／CHECKSUM／RESTORE VERIFYONLY及API126發布；Web、資產、health、version與test標籤通過。暫時dbcreator已撤除，sysadmin未授予，正式庫未連線。']];
const milestone = project.shapes.add({
  geometry: 'diamond',
  anchor: { from: { row: 300, col: 27, rowOffsetPx: 3, colOffsetPx: 2 }, extent: { widthPx: 8, heightPx: 8 } },
  fill: '#F59E0B',
  line: { style: 'solid', fill: '#B45309', width: 1 }
});
milestone.name = 'SPC-RELEASE-BACKUP-PERMISSION-1111-TASK-001 2026-11-11 測試發布完成';

workbook.recalculate();
const errors = (await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:3000})).ndjson;
if (!errors.includes('matched 0')) throw new Error('formula errors');
const image = await workbook.render({sheetName:'專案進度表',range:'A299:AK302',scale:1.2,format:'png'});
await fs.writeFile(preview, new Uint8Array(await image.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A287:K287',include:'values,formulas',tableMaxRows:2,tableMaxCols:11,maxChars:8000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:'專案進度表!A299:AK302',include:'values,formulas',tableMaxRows:6,tableMaxCols:40,maxChars:12000})).ndjson);
console.log((await workbook.inspect({kind:'drawing',sheetId:'專案進度表',maxChars:100000})).ndjson.includes(milestone.name)?'milestone=ok':'milestone=missing');
