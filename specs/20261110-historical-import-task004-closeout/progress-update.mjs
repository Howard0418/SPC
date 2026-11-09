import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [source, output] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const project = workbook.worksheets.getItem('專案進度表');
const tasks = workbook.worksheets.getItem('小工作總表');
const excelDay = (Date.UTC(2026, 10, 10) - Date.UTC(1899, 11, 30)) / 86400000;

tasks.getRange('E252').values = [['完成／合格咬蝕正式匯入核銷']];
tasks.getRange('F252').values = [['否']];
tasks.getRange('G252').values = [[excelDay]];
tasks.getRange('G252').setNumberFormat('yyyy-mm-dd');
tasks.getRange('H252').values = [['11/10重核：九／十月134份合格日報10,384筆已逐點對上正式資料；正式新增65份4,565筆、既有69份保持，重跑134略過／0新增。']];
tasks.getRange('I252').values = [['四線十月A面16／15／12／18子組與每面25／50樣本通過。9份異常及15份X依規則隔離／排除，是Invalid Input驗收通過，不重匯、不改來源或正式資料；藥液及TransFiles人工驗收仍為其他待辦。']];
tasks.getRange('J252').values = [['D:/SPC/specs/20261110-historical-import-task004-closeout/verification.md']];
tasks.getRange('A252:K252').format.rowHeight = 92;

tasks.getRange('A1').values = [['小工作總表（截至2026-11-10；九十月合格咬蝕正式匯入核銷）']];
tasks.getRange('D2').values = [[34]];
tasks.getRange('F2').values = [[205]];
tasks.getRange('A3').values = [['核心未完成34件，SPC相關10件；SPC-HISTORICAL-IMPORT-1102-TASK-004已核銷：134份合格咬蝕10,384筆、四線圖表與防重通過。9異常／15X維持來源清單；藥液及人工驗收另案。']];

project.getRange('A235:AK238').copyTo(project.getRange('A239:AK242'), 'all');
project.getRange('A239').values = [['2026-11-10：九／十月合格咬蝕正式匯入核銷']];
project.getRange('A240').values = [['系統']];
project.getRange('C240').values = [['工作項目']];
project.getRange('M240').values = [['實際日期']];
project.getRange('S240').values = [['狀態']];
project.getRange('AA240').values = [['11/10 核銷里程碑']];
project.getRange('A241').values = [['SPC']];
project.getRange('C241').values = [['◆ 九十月合格咬蝕正式匯入']];
project.getRange('M241').values = [[excelDay]];
project.getRange('M241').setNumberFormat('yyyy-mm-dd');
project.getRange('S241').values = [['完成／核銷']];
project.getRange('AA241').values = [['◆ 134份／10,384筆防重通過']];
project.getRange('A242').values = [['合格134份10,384筆逐點、線別、日期、25／50樣本及十月四線圖表通過；重跑0新增。9異常與15X維持隔離／排除，不視為合格缺漏。本次只核銷既有成果，未讀寫資料庫或發布網站。']];
project.getRange('AI5').values = [[excelDay]];
project.getRange('AI5').setNumberFormat('yyyy-mm-dd');
project.getRange('AJ5').values = [['11/10九十月合格咬蝕正式匯入核銷；SPC未完成10件']];
project.getRange('AK5').values = [['134份合格日報10,384筆、四線圖表與重送0新增證據完整；9異常／15X按規則拒絕，不重匯、不改資料。']];

const milestone = project.shapes.add({
  geometry: 'diamond',
  anchor: { from: { row: 240, col: 27, rowOffsetPx: 3, colOffsetPx: 2 }, extent: { widthPx: 8, heightPx: 8 } },
  fill: '#22C55E',
  line: { style: 'solid', fill: '#15803D', width: 1 }
});
milestone.name = 'SPC-HISTORICAL-IMPORT-1102-TASK-004 2026-11-10 完成';

workbook.recalculate();
for (const range of ['專案進度表!A235:AK242', '小工作總表!A1:K3', '小工作總表!A252:K252']) {
  console.log((await workbook.inspect({ kind: 'table', range, include: 'values,formulas', tableMaxRows: 12, tableMaxCols: 40, maxChars: 12000 })).ndjson);
}
console.log((await workbook.inspect({ kind: 'match', searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!', options: { useRegex: true, maxResults: 100 }, maxChars: 3000 })).ndjson);

await fs.mkdir('D:/SPC/.codex-progress-work/historical004-closeout', { recursive: true });
for (const [sheetName, range, name] of [
  ['專案進度表', 'A235:AK242', 'project'],
  ['小工作總表', 'A250:K253', 'tasks'],
]) {
  const image = await workbook.render({ sheetName, range, scale: 1.2, format: 'png' });
  await fs.writeFile(`D:/SPC/.codex-progress-work/historical004-closeout/${name}.png`, new Uint8Array(await image.arrayBuffer()));
}
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
