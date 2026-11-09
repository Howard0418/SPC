import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const workbook = await SpreadsheetFile.importXlsx(await FileBlob.load(process.argv[2]));
for (const range of ['小工作總表!A1:K300', '專案進度表!A231:AK242']) {
  const result = await workbook.inspect({ kind: 'table', range, include: 'values,formulas', tableMaxRows: 300, tableMaxCols: 40, maxChars: 250000 });
  const data = JSON.parse(result.ndjson);
  if (range.startsWith('小工作')) {
    const rows = data.values.slice(4).filter(row => row?.[2]);
    console.log(JSON.stringify({
      header: data.values.slice(0, 3),
      counts: {
        registered: rows.length,
        open: rows.filter(row => row?.[5] === '是').length,
        closed: rows.filter(row => row?.[5] === '否').length,
        spcOpen: rows.filter(row => row?.[5] === '是' && String(row?.[1] ?? '').includes('SPC')).length,
      },
      taskRow: data.values.findIndex(row => row?.[2] === 'SPC-HISTORICAL-IMPORT-1102-TASK-004') + 1,
      task: data.values.find(row => row?.[2] === 'SPC-HISTORICAL-IMPORT-1102-TASK-004')
    }));
  } else {
    console.log(result.ndjson);
  }
}
