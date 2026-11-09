import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const source = process.argv[2];
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
for (const range of ['專案進度表!A231:AK238', '小工作總表!A262:K262']) {
  console.log((await workbook.inspect({ kind: 'table', range, include: 'values,formulas', tableMaxRows: 10, tableMaxCols: 40, maxChars: 9000 })).ndjson);
}
console.log((await workbook.inspect({ kind: 'drawing', sheetId: workbook.worksheets.getItem('專案進度表').id, maxChars: 12000 })).ndjson);
console.log((await workbook.inspect({ kind: 'match', searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!', options: { useRegex: true, maxResults: 100 }, maxChars: 3000 })).ndjson);

await fs.mkdir('D:/SPC/.codex-progress-work/audit009-performance/verify', { recursive: true });
for (const [sheetName, range, file] of [
  ['專案進度表', 'A231:AK238', 'project'],
  ['小工作總表', 'A260:K263', 'tasks'],
]) {
  const image = await workbook.render({ sheetName, range, scale: 1.2, format: 'png' });
  await fs.writeFile(`D:/SPC/.codex-progress-work/audit009-performance/verify/${file}.png`, new Uint8Array(await image.arrayBuffer()));
}
