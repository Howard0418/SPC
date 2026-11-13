import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [source, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const values = tasks.getUsedRange(true).values;
const unfinished = values.slice(1).filter(row => row[5] === '是');
const spcRelated = unfinished.filter(row => String(row[1] ?? '').includes('SPC'));
console.log(JSON.stringify({unfinished: unfinished.length, spcRelated: spcRelated.length}));
console.log((await workbook.inspect({kind:'table',range:'小工作總表!A292:K292',include:'values,formulas',tableMaxRows:2,tableMaxCols:11,maxChars:8000})).ndjson);
console.log((await workbook.inspect({kind:'table',range:'專案進度表!A321:AK324',include:'values,formulas',tableMaxRows:5,tableMaxCols:37,maxChars:10000})).ndjson);
const image = await workbook.render({sheetName:'專案進度表',range:'A321:AK324',scale:1.2,format:'png'});
await fs.writeFile(preview, new Uint8Array(await image.arrayBuffer()));
