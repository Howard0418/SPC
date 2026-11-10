import fs from 'node:fs/promises';
import {FileBlob,SpreadsheetFile} from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const workbook=await SpreadsheetFile.importXlsx(await FileBlob.load(process.argv[2]));
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A1:K3',include:'values,formulas',tableMaxRows:3,tableMaxCols:11,maxChars:7000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A268:K290',include:'values,formulas',tableMaxRows:23,tableMaxCols:11,maxChars:20000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:'專案進度表!A235:AK260',include:'values,formulas',tableMaxRows:26,tableMaxCols:37,maxChars:20000})).ndjson);
const preview=await workbook.render({sheetName:'專案進度表',range:'A235:AK260',scale:1.1,format:'png'});
await fs.writeFile('D:/SPC/.november-daily-import-1110/progress-before.png',new Uint8Array(await preview.arrayBuffer()));
