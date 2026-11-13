import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [source, output, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');

tasks.getRange('I290').values=[['SPC歷史5/5、Portal UI12/12、build通過；11/09 N1／表處／氫氧化鉀為8.5→19.040，ID92273的9→20.160屬11/10；SPC0.1.127、Portal241／282及health/version=test。']];
tasks.getRange('K290').values=[['正式未發布；登入後畫面待使用者重新整理確認。日期依PortalDailyDate，非日報才回退MeasuredAt。']];
project.getRange('AB315').values=[['◆ SPC5／Portal12']];
project.getRange('A316').values=[['查詢初驗／複驗滴定值與新增頁同源；日報查詢日期採PortalDailyDate，避免將11/10的9／20.160列入11/09。11/09 N1／表處／氫氧化鉀正確為8.5／19.040；測試與build通過，SPC0.1.127、Portal241／282及health/version為test。正式未發布。']];

workbook.recalculate();
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A290:K290',include:'values,formulas',tableMaxRows:2,tableMaxCols:11,maxChars:8000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:'專案進度表!A313:AK316',include:'values,formulas',tableMaxRows:4,tableMaxCols:37,maxChars:10000})).ndjson);
console.log((await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:3000})).ndjson);
const image=await workbook.render({sheetName:'專案進度表',range:'A313:AK316',scale:1.2,format:'png'});
await fs.writeFile(preview,new Uint8Array(await image.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
