import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const [source, output, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');

tasks.getRange('E287:K287').values = [[
  'Portal／SPC測試與正式均已發布', '否', 46337,
  '查詢、管制圖與日報均採同列effectiveValue；複驗有值優先，否則初驗。完整鍵含日期、班別、取樣階段、線別、槽位與分析項目。',
  'Portal正式API1.0.231／Web1.0.272；SPC測試與正式API0.1.126。兩區Web／資產／API／環境、備份及VERIFYONLY通過。',
  'D:/SPC/specs/20261111-chemical-query-chart-value-alignment/spec.md',
  '2026/11/9 N1唯讀核對：表處／氫氧化鉀19.04與促進／氯化亞錫0.2256為不同PPC；兩環境一致。未重匯或改寫量測。'
]];
tasks.getRange('G287').setNumberFormat('yyyy-mm-dd');
project.getRange('C301').values = [['◆ 藥液有效分析值一致化正式發布完成']];
project.getRange('S301').values = [['測試／正式發布完成']];
project.getRange('AA301').values = [['◆ SPC API126正式完成']];
project.getRange('A302').values = [['Portal與SPC測試／正式均已發布；SPC正式API0.1.126／production，Web、資產、health、version及瀏覽器API基底通過。N1兩張截圖為不同槽位／分析項目，兩環境資料一致，未重匯或改寫量測。']];

workbook.recalculate();
const errors = (await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:3000})).ndjson;
if (!errors.includes('matched 0')) throw new Error('formula errors');
const image = await workbook.render({sheetName:'專案進度表',range:'A299:AK302',scale:1.2,format:'png'});
await fs.writeFile(preview, new Uint8Array(await image.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A287:K287',include:'values,formulas',tableMaxRows:2,tableMaxCols:11,maxChars:8000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:'專案進度表!A299:AK302',include:'values,formulas',tableMaxRows:6,tableMaxCols:40,maxChars:12000})).ndjson);
