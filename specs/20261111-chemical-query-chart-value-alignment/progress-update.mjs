import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const [source, output, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');

tasks.getRange('A287:K287').copyFrom(tasks.getRange('A286:K286'), 'all');
tasks.getRange('A287:K287').values = [[
  'P0', 'Portal／SPC', 'SPC-CHEM-QUERY-CHART-ALIGN-1111-TASK-001',
  '藥液查詢與管制圖分析值一致化', 'Portal測試站已發布／SPC API發布受阻', '是', 46337,
  '查詢頁直接使用SPC effectiveValue：複驗有值時優先，否則初驗；顯示班別與取樣階段，避免同日跨班別或開收線誤比。',
  'Portal Web契約2項、Web回歸41項、SPC API回歸96項及兩專案Release build通過。Portal測試API1.0.231／Web1.0.272已發布；SPC API0.1.126 Apply在必要DB備份階段安全停止。',
  'D:/SPC/specs/20261111-chemical-query-chart-value-alignment/spec.md',
  '正式未發布；量測、公式、F表、規格及DB未改。SPC測試API仍0.1.125，待具備PMR_SPC_TEST備份權限後重發；登入畫面因瀏覽器工具未連線待驗。'
]];
tasks.getRange('G287').setNumberFormat('yyyy-mm-dd');

project.getRange('A287:AK290').copyTo(project.getRange('A291:AK294'), 'all');
project.getRange('A291').values = [['2026-11-11：藥液查詢與管制圖分析值一致化']];
project.getRange('A292').values = [['系統']];
project.getRange('C292').values = [['工作項目']];
project.getRange('M292').values = [['實際日期']];
project.getRange('S292').values = [['狀態']];
project.getRange('AA292').values = [['11/11 測試里程碑']];
project.getRange('A293').values = [['Portal／SPC']];
project.getRange('C293').values = [['◆ 查詢採有效分析值並顯示班別／階段']];
project.getRange('M293').values = [[46337]];
project.getRange('M293').setNumberFormat('yyyy-mm-dd');
project.getRange('S293').values = [['Portal測試已發布／SPC受阻']];
project.getRange('AA293').values = [['◆ Portal231／272；SPC126待備份權限']];
project.getRange('A294').values = [['查詢以effectiveValue顯示複驗優先、否則初驗，並標示班別與取樣階段。Portal測試站連線通過；SPC API因測試庫備份權限不足在套用前安全停止，現行API125與資料保持。正式未發布，登入畫面待驗。']];
const milestone = project.shapes.add({
  geometry: 'diamond',
  anchor: { from: { row: 292, col: 27, rowOffsetPx: 3, colOffsetPx: 2 }, extent: { widthPx: 8, heightPx: 8 } },
  fill: '#F59E0B',
  line: { style: 'solid', fill: '#B45309', width: 1 }
});
milestone.name = 'SPC-CHEM-QUERY-CHART-ALIGN-1111-TASK-001 2026-11-11 Portal測試完成SPC API受阻';

workbook.recalculate();
const errors = (await workbook.inspect({
  kind: 'match',
  searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',
  options: { useRegex: true, maxResults: 100 },
  maxChars: 3000
})).ndjson;
if (!errors.includes('matched 0')) throw new Error('formula errors');
const image = await workbook.render({ sheetName: '專案進度表', range: 'A291:AK294', scale: 1.2, format: 'png' });
await fs.writeFile(preview, new Uint8Array(await image.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);

console.log((await workbook.inspect({ kind: 'table', range: '小工作總表!A287:K287', include: 'values,formulas', tableMaxRows: 2, tableMaxCols: 11, maxChars: 8000 })).ndjson);
console.log((await workbook.inspect({ kind: 'table', range: '專案進度表!A291:AK294', include: 'values,formulas', tableMaxRows: 6, tableMaxCols: 40, maxChars: 12000 })).ndjson);
console.log((await workbook.inspect({ kind: 'drawing', sheetId: '專案進度表', maxChars: 100000 })).ndjson.includes(milestone.name) ? 'milestone=ok' : 'milestone=missing');
