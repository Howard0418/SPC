import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const [source, output, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');

tasks.getRange('E287:K287').values = [[
  'Portal正式站已發布／SPC API測試發布受阻', '是', 46337,
  '查詢頁直接使用SPC effectiveValue：複驗有值時優先，否則初驗；顯示班別與取樣階段，避免同日跨班別或開收線誤比。',
  'Portal契約2項、Web回歸41項、SPC API回歸96項及兩專案Release build通過。Portal正式API1.0.231／Web1.0.272已完成Plan、備份、VERIFYONLY、Apply及連線驗證；SPC API0.1.126仍受測試庫備份權限阻擋。',
  'D:/SPC/specs/20261111-chemical-query-chart-value-alignment/spec.md',
  'Portal正式查詢修正已發布；SPC取樣階段契約尚未發布，測試API仍0.1.125。量測、公式、F表、規格及資料未改；登入後畫面仍待人工驗收。'
]];
tasks.getRange('G287').setNumberFormat('yyyy-mm-dd');

project.getRange('A291:AK294').copyTo(project.getRange('A295:AK298'), 'all');
project.getRange('A295').values = [['2026-11-11：藥液查詢有效分析值正式發布']];
project.getRange('A296').values = [['系統']];
project.getRange('C296').values = [['工作項目']];
project.getRange('M296').values = [['實際日期']];
project.getRange('S296').values = [['狀態']];
project.getRange('AA296').values = [['11/11 正式里程碑']];
project.getRange('A297').values = [['Portal／SPC']];
project.getRange('C297').values = [['◆ Portal查詢有效分析值正式發布']];
project.getRange('M297').values = [[46337]];
project.getRange('M297').setNumberFormat('yyyy-mm-dd');
project.getRange('S297').values = [['Portal正式已發布／SPC測試受阻']];
project.getRange('AA297').values = [['◆ Portal231／272正式；SPC126未套用']];
project.getRange('A298').values = [['正式Portal以既有正式230／271基準只加入本件查詢修正，升至API231／Web272；正式Plan、DB備份與VERIFYONLY、Apply、Web／資產／API正式環境驗證通過。SPC API126仍因測試庫備份權限不足未套用，不越過測試先行；量測與主檔資料未異動。']];
const milestone = project.shapes.add({
  geometry: 'diamond',
  anchor: { from: { row: 296, col: 27, rowOffsetPx: 3, colOffsetPx: 2 }, extent: { widthPx: 8, heightPx: 8 } },
  fill: '#2563EB',
  line: { style: 'solid', fill: '#1D4ED8', width: 1 }
});
milestone.name = 'SPC-CHEM-QUERY-CHART-ALIGN-1111-TASK-001 2026-11-11 Portal正式發布';

workbook.recalculate();
const errors = (await workbook.inspect({
  kind: 'match',
  searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',
  options: { useRegex: true, maxResults: 100 },
  maxChars: 3000
})).ndjson;
if (!errors.includes('matched 0')) throw new Error('formula errors');
const image = await workbook.render({ sheetName: '專案進度表', range: 'A295:AK298', scale: 1.2, format: 'png' });
await fs.writeFile(preview, new Uint8Array(await image.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);

console.log((await workbook.inspect({ kind: 'table', range: '小工作總表!A287:K287', include: 'values,formulas', tableMaxRows: 2, tableMaxCols: 11, maxChars: 8000 })).ndjson);
console.log((await workbook.inspect({ kind: 'table', range: '專案進度表!A295:AK298', include: 'values,formulas', tableMaxRows: 6, tableMaxCols: 40, maxChars: 12000 })).ndjson);
console.log((await workbook.inspect({ kind: 'drawing', sheetId: '專案進度表', maxChars: 100000 })).ndjson.includes(milestone.name) ? 'milestone=ok' : 'milestone=missing');
