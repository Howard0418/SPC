import fs from 'node:fs/promises';
import { Workbook, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';

const root = 'D:/SPC/specs/20261118-chemical-test-production-comparison';
const outDir = 'D:/SPC/outputs/spc-chemical-env-comparison-20261118';
const output = `${outDir}/SPC測試正式藥液資料差異_20261118.xlsx`;
const data = JSON.parse(await fs.readFile(`${root}/comparison.json`, 'utf8'));
await fs.mkdir(outDir, { recursive: true });

const wb = Workbook.create();
const font = 'Arial';
const navy = '#17365D', blue = '#D9EAF7', light = '#F3F6F9', red = '#FCE8E6', amber = '#FFF2CC', green = '#E2F0D9';

function columns(rows) {
  const list=[]; const seen=new Set();
  for (const row of rows) for (const k of Object.keys(row)) if (!seen.has(k)) { seen.add(k); list.push(k); }
  return list;
}
function writeTable(sheet, startRow, rows, name, preferred=[]) {
  const found=columns(rows); const cols=[...preferred.filter(x=>found.includes(x)),...found.filter(x=>!preferred.includes(x))];
  const matrix=[cols, ...rows.map(r=>cols.map(c=>r[c] ?? null))];
  if (!rows.length) matrix.push(cols.map(()=>null));
  const endRow=startRow+matrix.length-1, endCol=cols.length;
  const range=sheet.getRangeByIndexes(startRow-1,0,matrix.length,cols.length); range.values=matrix;
  sheet.tables.add(`A${startRow}:${columnName(endCol)}${endRow}`,true,name).style='TableStyleMedium2';
  range.format.font={name:font,size:9}; range.format.verticalAlignment='center';
  sheet.getRange(`A${startRow}:${columnName(endCol)}${startRow}`).format={fill:navy,font:{name:font,size:9,bold:true,color:'#FFFFFF'},horizontalAlignment:'center',verticalAlignment:'center',wrapText:true};
  sheet.freezePanes.freezeRows(startRow); sheet.showGridLines=false;
  return {cols,endRow,endCol};
}
function columnName(n){let s='';while(n){n--;s=String.fromCharCode(65+n%26)+s;n=Math.floor(n/26)}return s}
function setWidths(sheet, cols, defaults={}) {
  cols.forEach((c,i)=>{sheet.getRange(`${columnName(i+1)}:${columnName(i+1)}`).format.columnWidth=defaults[c]??14;});
}
function title(sheet,text,subtitle){
  sheet.getRange('A2').values=[[text]];sheet.getRange('A2').format.font={name:font,size:15,bold:true,color:navy};
  sheet.getRange('A3').values=[[subtitle]];sheet.getRange('A3').format.font={name:font,size:9,italic:true,color:'#666666'};
}

const summary=wb.worksheets.add('摘要'); summary.tabColor=navy; summary.showGridLines=false;
title(summary,'SPC 測試／正式藥液資料差異','唯讀查詢；測試 PMR_SPC_TEST 對正式 PMR_SPC_2026；產生時間 '+data.generatedAt);
const c=data.controls;
summary.getRange('A5:H7').values=[
 ['比較項目','測試','正式','差異列數','說明',null,null,null],
 ['CHEM管制項目',c.testMappings,c.productionMappings,c.masterDifferenceRows,'自然鍵：線別／製程／槽體／分析項目',null,null,null],
 ['公式有效區間',c.testEffectivities,c.productionEffectivities,c.formulaDifferenceRows,'含日期區間、公式雜湊、F版本／雜湊',null,null,null]
];
summary.getRange('A8:E8').values=[['量測實體列',c.testMeasurements,c.productionMeasurements,c.measurementDifferenceRows,`有效資料共同鍵 ${c.activeCommonKeys} 組，分析值一致 ${c.activeValueEqualKeys} 組、不同 ${c.activeValueDifferentKeys} 組；明細另含封存資料`]];
summary.getRange('A5:E5').format={fill:navy,font:{name:font,size:10,bold:true,color:'#FFFFFF'},horizontalAlignment:'center'};
summary.getRange('A5:E8').format.borders={preset:'outside',style:'thin',color:'#B8C6D1'};
summary.getRange('A6:E8').format.font={name:font,size:10};
summary.getRange('A10').values=[['重要發現']];summary.getRange('A10:E10').merge();summary.getRange('A10').format={fill:blue,font:{name:font,size:11,bold:true,color:navy}};
const limitDiff=data.masterDifferences.filter(x=>['USL','LSL','TargetValue'].includes(x.Field));
const testOnlyMaster=data.masterDifferences.filter(x=>x.Difference==='TestOnly');
summary.getRange('A11:E16').values=[
 ['1','正式15條業務線別在測試區都有資料；測試另有DEMO-FORMULA-20261021測試線別。',null,null,null],
 ['2',`啟用管制項目數：測試 ${data.lineSummary.reduce((s,x)=>s+(x.Test_EnabledMappings||0),0)}、正式 ${data.lineSummary.reduce((s,x)=>s+(x.Production_EnabledMappings||0),0)}；各正式線別啟用數量一致。`,null,null,null],
 ['3',`規格值差異共 ${limitDiff.length} 欄，皆為QE2／TANK-000281／CHAR-000836：測試LSL/Target/USL=4/7/10，正式=3/6/9。`,null,null,null],
 ['4',`測試另有 ${testOnlyMaster.length} 組管制項目自然鍵，主要為隔離測試資料；詳見「主檔差異」。`,null,null,null],
 ['5',`只看兩區目前有效量測，相同自然鍵共 ${c.activeCommonKeys} 組，其中初／複驗分析值一致 ${c.activeValueEqualKeys} 組、不同 ${c.activeValueDifferentKeys} 組；封存資料另列，不混入有效值結論。`,null,null,null],
 ['6','量測歷史起訖不同，且測試含大量封存／刪除測試資料；不可直接用總筆數判定正式缺資料。請依「量測線別摘要」共同日期區間及「量測差異」逐筆判讀。',null,null,null]
];
summary.getRange('A11:A16').format.font={name:font,bold:true,color:navy};summary.getRange('B11:B16').format={font:{name:font,size:10},wrapText:true};
const lineStart=19; const lineInfo=writeTable(summary,lineStart,data.lineSummary,'LineSummaryTable',['Line','InTest','InProduction','Test_EnabledMappings','Production_EnabledMappings','Test_MeasurementsActive','Production_MeasurementsActive','Test_MeasurementsDeleted','Production_MeasurementsDeleted','Test_FirstMeasuredAt','Production_FirstMeasuredAt','Test_LastMeasuredAt','Production_LastMeasuredAt']);
setWidths(summary,lineInfo.cols,{Line:22,Test_FirstMeasuredAt:20,Production_FirstMeasuredAt:20,Test_LastMeasuredAt:20,Production_LastMeasuredAt:20});
summary.getRange('A:A').format.columnWidth=22;summary.getRange('B:B').format.columnWidth=78;summary.getRange('C:E').format.columnWidth=16;
summary.freezePanes.freezeRows(lineStart);

const ms=wb.worksheets.add('主檔差異');title(ms,'管制項目主檔差異','欄位差異以自然鍵比對；ID不同不列為差異。');
const mi=writeTable(ms,5,data.masterDifferences,'MasterDiffTable',['Difference','MachineCode','ProcessCode','TankCode','CharacteristicCode','Field','TestValue','ProductionValue','TestCount','ProductionCount']);
setWidths(ms,mi.cols,{Difference:20,MachineCode:18,ProcessCode:22,TankCode:28,CharacteristicCode:28,Field:22,TestValue:34,ProductionValue:34});
ms.getRange(`A6:${columnName(mi.endCol)}${mi.endRow}`).conditionalFormats.addCustom('=$A6="TestOnly"',{fill:amber});
ms.getRange(`A6:${columnName(mi.endCol)}${mi.endRow}`).conditionalFormats.addCustom('=$A6="ProductionOnly"',{fill:red});

const fsheet=wb.worksheets.add('公式與F差異');title(fsheet,'公式排程與 F 差異','比較日期有效區間、公式設定雜湊、F歷史版本與F參照；不顯示敏感連線設定。');
const fi=writeTable(fsheet,5,data.formulaDifferences,'FormulaDiffTable',['Category','Difference','MachineCode','ProcessCode','TankCode','CharacteristicCode','EffectiveFromUtc','EffectiveUntilUtcExclusive','Field','TestValue','ProductionValue','TestCount','ProductionCount']);
setWidths(fsheet,fi.cols,{Category:16,Difference:20,MachineCode:18,ProcessCode:22,TankCode:26,CharacteristicCode:26,EffectiveFromUtc:21,EffectiveUntilUtcExclusive:21,Field:22,TestValue:40,ProductionValue:40});

const ml=wb.worksheets.add('量測線別摘要');title(ml,'量測差異（按線別）','共同日期區間僅用於辨識可比較範圍；詳細差異仍保留有效及封存資料狀態。');
const mli=writeTable(ml,5,data.measurementLineSummary,'MeasurementLineTable',['Line','CommonStart','CommonEnd','TestRowsInOverlap','ProductionRowsInOverlap','ActiveCommonKeys','ActiveValueEqualKeys','ActiveValueDifferentKeys','ActiveTestOnlyKeys','ActiveProductionOnlyKeys']);
setWidths(ml,mli.cols,{Line:18,CommonStart:22,CommonEnd:22});

const md=wb.worksheets.add('量測差異');title(md,'量測逐筆差異','自然鍵：線別／製程／槽體／分析項目／量測時間／樣本／階段／日報日期；相同鍵但內容不同以JSON並列。');
const mdi=writeTable(md,5,data.measurementDifferences,'MeasurementDiffTable',['Difference','MachineCode','ProcessCode','TankCode','CharacteristicCode','MeasuredAt','SampleNo','SamplingPhase','SamplingStage','PortalDailyDate','MeasuredValue','RecheckValue','IsDeleted','HasReceipt','TraceCount','TestValues','ProductionValues']);
setWidths(md,mdi.cols,{Difference:24,MachineCode:18,ProcessCode:22,TankCode:26,CharacteristicCode:26,MeasuredAt:21,SamplingPhase:16,SamplingStage:16,PortalDailyDate:18,TestValues:60,ProductionValues:60,AdjustAmount:34,SourceReference:32});
md.getRange(`A6:${columnName(mdi.endCol)}${mdi.endRow}`).conditionalFormats.addCustom('=$A6="TestOnly"',{fill:amber});
md.getRange(`A6:${columnName(mdi.endCol)}${mdi.endRow}`).conditionalFormats.addCustom('=$A6="ProductionOnly"',{fill:red});

for (const sheet of wb.worksheets.items) sheet.getUsedRange(true).format.verticalAlignment='center';
wb.recalculate();
const errors=await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:2000});
if(!errors.ndjson.includes('matched 0 entries')) throw new Error(errors.ndjson);
const inspect=await wb.inspect({kind:'table',range:'摘要!A2:H25',include:'values,formulas',tableMaxRows:25,tableMaxCols:8,maxChars:10000});
await fs.writeFile(`${root}/report-inspect.ndjson`,inspect.ndjson);
const preview=await wb.render({sheetName:'摘要',range:'A1:M34',scale:1,format:'png'});
await fs.writeFile(`${root}/report-preview.png`,new Uint8Array(await preview.arrayBuffer()));
await (await SpreadsheetFile.exportXlsx(wb)).save(output);
console.log(JSON.stringify({output,sheets:wb.worksheets.items.map(x=>x.name),formulaErrors:0,rows:{master:data.masterDifferences.length,formula:data.formulaDifferences.length,measurement:data.measurementDifferences.length}},null,2));
