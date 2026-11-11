import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const [source, output] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');

tasks.getRange('E286').values = [['正式已發布／登入後日報畫面待人工驗收']];
tasks.getRange('H286').values = [['SPC排程明示公式是否引用Secondary；Portal只在真正雙輸入公式顯示第二原始值。daily API優先由最新Trace／Receipt回填；保存欄位有值必顯示，明確空值保持空白，只有欄位不存在才回退UploadDetail。']];
tasks.getRange('I286').values = [['相關API 15項通過；正式SPC API0.1.125／Web0.1.127、Portal API1.0.230／Web1.0.271 已發布。正式Web／JS／CSS MIME、health／version及production環境通過；登入後日報畫面待人工驗收。']];
tasks.getRange('K286').values = [['未修改公式、F值、判定或量測資料；未重匯。依授權套用既有稽核／界限歷程及語系偏好migration。']];

project.getRange('S289').values = [['正式已發布／登入後日報畫面待人工驗收']];
project.getRange('AA289').values = [['◆ SPC125／Portal271 保存值顯示規則']];
project.getRange('A290').values = [['日報由最新Trace／Receipt回填原始滴定值；保存欄位有值必顯示，明確空值保持空白，只有欄位不存在才相容UploadDetail。正式SPC API0.1.125／Web0.1.127、Portal API1.0.230／Web1.0.271已發布；Web／資產／API與production連線通過。未重匯或改寫資料，登入後畫面待人工驗收。']];

workbook.recalculate();
for (const range of ['小工作總表!A282:K286', '專案進度表!A287:AK290']) {
  console.log((await workbook.inspect({ kind:'table', range, include:'values,formulas', tableMaxRows:12, tableMaxCols:40, maxChars:14000 })).ndjson);
}
console.log((await workbook.inspect({ kind:'match', searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!', options:{useRegex:true,maxResults:100}, maxChars:3000 })).ndjson);
const preview = await workbook.render({ sheetName:'小工作總表', range:'A282:K286', scale:1.2, format:'png' });
await fs.writeFile(new URL('progress-after.png', import.meta.url), new Uint8Array(await preview.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
