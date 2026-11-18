import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from 'file:///C:/Users/ihao_ting.PMR.000/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const root='D:/SPC/specs/20261118-chemical-test-production-comparison';
const file='D:/SPC/outputs/spc-chemical-env-comparison-20261118/SPC測試正式藥液資料差異_20261118.xlsx';
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(file));
const checks={};
for(const [sheet,range] of [['摘要','A1:M34'],['主檔差異','A1:J16'],['公式與F差異','A1:M16'],['量測線別摘要','A1:J22'],['量測差異','A1:Q16']]){
 const img=await wb.render({sheetName:sheet,range,scale:0.8,format:'png'});
 await fs.writeFile(`${root}/verify-${sheet}.png`,new Uint8Array(await img.arrayBuffer()));
 const s=wb.worksheets.getItem(sheet); checks[sheet]={rows:s.getUsedRange(true).values.length,cols:s.getUsedRange(true).values[0]?.length||0};
}
const errors=await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!',options:{useRegex:true,maxResults:100},maxChars:2000});
if(!errors.ndjson.includes('matched 0 entries')) throw new Error(errors.ndjson);
console.log(JSON.stringify({checks,formulaErrors:0},null,2));
