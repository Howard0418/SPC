import fs from 'node:fs/promises';
import {Workbook,SpreadsheetFile} from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const [input,output,previewDir]=process.argv.slice(2);const data=JSON.parse(await fs.readFile(input,'utf8'));
const wb=Workbook.create();const summary=wb.worksheets.add('摘要');const chem=wb.worksheets.add('藥液對帳');const etch=wb.worksheets.add('咬蝕量對帳');
const font='Arial',navy='#1F4E78',blue='#D9EAF7',amber='#FFF2CC',red='#FCE4D6',green='#E2F0D9';
for(const s of [summary,chem,etch]){s.showGridLines=false;s.getRange('A1:Z1000').format.font={name:font,size:10};}
summary.getRange('A2').values=[['2026/11/01～11/10 測試資料庫唯讀防重對帳']];summary.getRange('A2').format.font={name:font,size:15,bold:true,color:'#1F1F1F'};
summary.getRange('A4:D4').values=[['資料類別','可新增','已存在相同','同鍵異值／錯誤']];
summary.getRange('A5:D6').values=[
 ['藥液',data.chemical.counts.insert_candidate||0,data.chemical.counts.existing_same||0,(data.chemical.counts.same_key_different_content||0)+(data.chemical.mappingErrors?.length||0)+(data.chemical.counts.duplicate_existing_key||0)],
 ['咬蝕量',data.etching.counts.insert_candidate||0,data.etching.counts.existing_same||0,(data.etching.counts.same_key_different_content||0)+(data.etching.counts.duplicate_existing_key||0)]
];
summary.getRange('A8:D12').values=[['檢查項目','結果','範圍','說明'],['資料庫','唯讀','PMR_SPC_TEST／PMR_PORTAL_TEST','沒有INSERT、UPDATE或DELETE'],['藥液主檔映射','通過',data.chemical.sourceRows+' 筆','無法映射 0 筆'],['咬蝕來源完整性','待修正','21份／1,750點','11/10 QE1缺S4/S14仍隔離，不在21份內'],['後續動作','待確認','測試庫匯入',`同鍵異值${data.chemical.counts.same_key_different_content||0}筆不可自動覆蓋；先核對明細`]];
for(const range of ['A4:D4','A8:D8']){summary.getRange(range).format={fill:navy,font:{name:font,bold:true,color:'#FFFFFF'},horizontalAlignment:'center',verticalAlignment:'center',borders:{preset:'all',style:'thin',color:'#FFFFFF'}};}
summary.getRange('A5:D6').format.borders={preset:'all',style:'thin',color:'#D9E2F3'};summary.getRange('A9:D12').format.borders={preset:'all',style:'thin',color:'#D9E2F3'};summary.getRange('A5:D5').format.fill=blue;summary.getRange('D5').format.fill=red;summary.getRange('B5:B6').format.fill=green;
summary.getRange('A1:D20').format.verticalAlignment='center';summary.getRange('A:A').format.columnWidth=24;summary.getRange('B:B').format.columnWidth=24;summary.getRange('C:C').format.columnWidth=34;summary.getRange('D:D').format.columnWidth=62;summary.getRange('A2:D2').format.rowHeight=28;

const chemHeaders=['分類','日期','線別／PPC','班別','取樣階段','Excel初驗值','測試庫初驗值','Excel複驗值','測試庫複驗值','來源檔','Sheet','列號','業務鍵'];
chem.getRange('A2:M2').values=[chemHeaders];
const chemRows=data.chemical.rows.map(x=>{const s=x.sourceRecord||{},e=(x.existingRecords||[])[0]||{},src=s.source||{};const parts=String(x.key).split('|');return [x.classification,parts[1]||'',parts[0]||'',parts[2]||'',parts[3]||'',s.value??null,e.value??null,s.recheckValue??null,e.recheckValue??null,src.file||'',src.sheet||'',src.row??null,x.key];});
if(chemRows.length)chem.getRangeByIndexes(2,0,chemRows.length,13).values=chemRows;
chem.getRange('A2:M2').format={fill:navy,font:{name:font,bold:true,color:'#FFFFFF'},horizontalAlignment:'center',verticalAlignment:'center'};chem.freezePanes.freezeRows(2);chem.getRange('A:M').format.columnWidth=16;chem.getRange('J:J').format.columnWidth=24;chem.getRange('K:K').format.columnWidth=18;chem.getRange('M:M').format.columnWidth=42;chem.getRange(`F3:I${chemRows.length+2}`).setNumberFormat('0.00000000');
chem.getRange(`A3:A${chemRows.length+2}`).conditionalFormats.add('containsText',{text:'same_key_different_content',format:{fill:red,font:{color:'#9C0006',bold:true}}});chem.getRange(`A3:A${chemRows.length+2}`).conditionalFormats.add('containsText',{text:'insert_candidate',format:{fill:amber}});chem.getRange(`A3:A${chemRows.length+2}`).conditionalFormats.add('containsText',{text:'existing_same',format:{fill:green}});

const etchHeaders=['分類','日期','線別','樣本點數','來源Sheet','測試庫報表ID','業務鍵'];etch.getRange('A2:G2').values=[etchHeaders];
const etchRows=data.etching.rows.map(x=>{const s=x.sourceRecord||{},e=(x.existingRecords||[])[0]||{},src=s.source||{};const parts=String(x.key).split('|');return [x.classification,parts[0]||'',parts[1]||'',(s.points||[]).length,src.sheet||'',e.source?.id||'',x.key];});
if(etchRows.length)etch.getRangeByIndexes(2,0,etchRows.length,7).values=etchRows;etch.getRange('A2:G2').format={fill:navy,font:{name:font,bold:true,color:'#FFFFFF'},horizontalAlignment:'center',verticalAlignment:'center'};etch.freezePanes.freezeRows(2);etch.getRange('A:G').format.columnWidth=18;etch.getRange('E:E').format.columnWidth=22;etch.getRange('G:G').format.columnWidth=28;etch.getRange(`A3:A${etchRows.length+2}`).format.fill=amber;
wb.recalculate();await fs.mkdir(previewDir,{recursive:true});for(const [s,r,n] of [[summary,'A1:D13','summary.png'],[chem,'A1:M20','chemical.png'],[etch,'A1:G24','etch.png']]){const p=await wb.render({sheetName:s.name,range:r,scale:1.1,format:'png'});await fs.writeFile(`${previewDir}/${n}`,new Uint8Array(await p.arrayBuffer()));}
console.log((await wb.inspect({kind:'table',range:'摘要!A2:D12',include:'values,formulas',tableMaxRows:12,tableMaxCols:4,maxChars:8000})).ndjson);console.log((await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:3000})).ndjson);
await fs.mkdir(output.substring(0,output.lastIndexOf('\\')), {recursive:true});await (await SpreadsheetFile.exportXlsx(wb)).save(output);
