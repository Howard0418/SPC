const ExcelJS = require('exceljs');
const wb = new ExcelJS.Workbook();
wb.xlsx.readFile('C:/Users/ihao_ting/Desktop/MES/docs/藥液分析日報表.xlsx').then(() => {
  wb.eachSheet(ws => {
    console.log('\n=== Sheet: ' + ws.name + ' (rows:' + ws.rowCount + ', cols:' + ws.columnCount + ') ===');
    ws.eachRow({includeEmpty: false}, (row, rn) => {
      if (rn <= 35) {
        const vals = [];
        row.eachCell({includeEmpty: true}, (cell, cn) => {
          vals.push('[' + cn + ']' + (cell.value !== null && cell.value !== undefined ? String(cell.value).substring(0,40) : ''));
        });
        console.log('Row ' + rn + ': ' + vals.join('  '));
      }
    });
  });
}).catch(e => console.error(e.message));
