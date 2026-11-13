import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [mode, source, output, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const project = workbook.worksheets.getItem('專案進度表');

if (mode === 'inspect') {
  console.log((await workbook.inspect({kind:'table',range:'小工作總表!A285:K300',include:'values,formulas',tableMaxRows:20,tableMaxCols:11,maxChars:16000})).ndjson);
  console.log((await workbook.inspect({kind:'table',range:'專案進度表!A310:AK325',include:'values,formulas',tableMaxRows:20,tableMaxCols:37,maxChars:16000})).ndjson);
  const image=await workbook.render({sheetName:'專案進度表',range:'A310:AK325',scale:1.2,format:'png'});
  await fs.writeFile(preview,new Uint8Array(await image.arrayBuffer()));
  process.exit(0);
}

tasks.getRange('A291:K291').copyFrom(tasks.getRange('A290:K290'),'all');
project.getRange('A317:AK320').copyFrom(project.getRange('A313:AK316'),'all');

tasks.getRange('A291:K291').values=[[
  'P0','SPC','SPC-CHEM-CHART-STAGE-LABEL-1113-TASK-001','藥液管制圖分辨開線／收線','測試站已發布／正式未發布','是',46339,
  '同日早班點依保存SamplingStage顯示早(開)／早(收)；保留單一序列與既有統計。',
  '焦點測試8/8、i18n及Web build通過；SPC測試Web0.1.129、API0.1.127/test，首頁與JS/CSS MIME、同源health/version通過。',
  'D:/SPC/specs/20261113-chemical-chart-stage-label/verification.md',
  '正式未發布；原生瀏覽器操作管道不可用，登入畫面待人工重新整理驗收。'
]];
project.getRange('A317').values=[['2026-11-13：藥液管制圖開收線辨識']];
project.getRange('A318:AK318').values=[['系統',null,'工作項目',null,null,null,null,null,null,null,null,null,'實際日期',null,null,null,null,null,'狀態',null,null,null,null,null,null,null,null,'11/13 測試里程碑',null,null,null,null,null,null,null,null,null]];
project.getRange('A319:AK319').values=[['SPC',null,'◆ 管制圖開收線辨識',null,null,null,null,null,null,null,null,null,46339,null,null,null,null,null,'測試站已發布／正式未發布',null,null,null,null,null,null,null,null,'◆ UI8／Web129',null,null,null,null,null,null,null,null,null]];
project.getRange('A320').values=[['同日早班開線／收線依SamplingStage顯示早(開)／早(收)，主檔投影不完整時仍可辨識；單一序列、MR、管制線、Cpk及資料保持。焦點測試8/8與build通過，測試Web0.1.129、API0.1.127/test。正式未發布。']];

workbook.recalculate();
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A290:K291',include:'values,formulas',tableMaxRows:3,tableMaxCols:11,maxChars:10000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:'專案進度表!A317:AK320',include:'values,formulas',tableMaxRows:5,tableMaxCols:37,maxChars:12000})).ndjson);
console.log((await workbook.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:3000})).ndjson);
const image=await workbook.render({sheetName:'專案進度表',range:'A317:AK320',scale:1.2,format:'png'});
await fs.writeFile(preview,new Uint8Array(await image.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
