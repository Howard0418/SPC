import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const [source, output, preview] = process.argv.slice(2);
const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(source));
const tasks = workbook.worksheets.getItem('小工作總表');
const day = (Date.UTC(2026, 10, 13) - Date.UTC(1899, 11, 30)) / 86400000;

tasks.getRange('A1').values = [['小工作總表（截至2026-11-13；CAL-VERIFY-002 使用者確認取消）']];
tasks.getRange('D2').values = [[36]];
tasks.getRange('A3').values = [['核心未完成36件，SPC相關12件；CAL-VERIFY-002 已由使用者確認資料正確並取消執行，不偽造59列原始證據。人工12件／待核銷4件另計。']];
tasks.getRange('E11:K11').values = [['已取消／使用者確認資料正確', '否', day, '使用者於2026-11-13確認資料正確；不執行原始 Excel 恢復。', '原始 Excel 保持缺失；不偽造59列原始證據。', 'D:/SPC/specs/20261113-cal-verify-002-closeout/spec.md', '使用者確認／取消']];
tasks.getRange('G11').setNumberFormat('yyyy-mm-dd');

workbook.recalculate();
console.log((await workbook.inspect({ kind: 'table', range: '小工作總表!A1:K12', include: 'values,formulas', tableMaxRows: 12, tableMaxCols: 11, maxChars: 10000 })).ndjson);
console.log((await workbook.inspect({ kind: 'match', searchTerm: '#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!', options: { useRegex: true, maxResults: 100 }, maxChars: 3000 })).ndjson);
const image = await workbook.render({ sheetName: '小工作總表', range: 'A1:K12', scale: 1.2, format: 'png' });
await fs.writeFile(preview, new Uint8Array(await image.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(workbook)).save(output);
