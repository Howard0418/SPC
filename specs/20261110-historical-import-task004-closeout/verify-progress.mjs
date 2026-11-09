import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(process.argv[2]));
for (const range of ['專案進度表!A235:AK242', '小工作總表!A1:K3', '小工作總表!A252:K252']) {
  console.log((await workbook.inspect({ kind: 'table', range, include: 'values,formulas', tableMaxRows: 12, tableMaxCols: 40, maxChars: 12000 })).ndjson);
}
console.log((await workbook.inspect({ kind: 'drawing', sheetId: workbook.worksheets.getItem('專案進度表').id, maxChars: 60000 })).ndjson);
console.log((await workbook.inspect({ kind: 'match', searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!', options: { useRegex: true, maxResults: 100 }, maxChars: 3000 })).ndjson);

await fs.mkdir('D:/SPC/.codex-progress-work/historical004-closeout/verify', { recursive: true });
for (const [sheetName, range, name] of [
  ['專案進度表', 'A235:AK242', 'project'],
  ['小工作總表', 'A250:K253', 'tasks'],
]) {
  const image = await workbook.render({ sheetName, range, scale: 1.2, format: 'png' });
  await fs.writeFile(`D:/SPC/.codex-progress-work/historical004-closeout/verify/${name}.png`, new Uint8Array(await image.arrayBuffer()));
}
