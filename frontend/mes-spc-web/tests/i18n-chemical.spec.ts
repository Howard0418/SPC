import {test,expect,Page} from '@playwright/test';
import * as XLSX from 'xlsx';
import {readFile} from 'node:fs/promises';
const raw='原名 <b>@:key {name}</b>';
const config={enabled:true,concentrationFormula:'Primary * F!B3 + 0.2',adjustmentFormula:'10 - Primary',adjustmentAmountFormula:'Primary * 2',decimalPlaces:2,secondaryInputLabel:raw,primaryInputLabel:'滴定值',original:{text:raw}};
const items=Array.from({length:12},(_,i)=>({id:i+1,controlScope:'CHEM',partId:null,processId:21,machineId:31,tankId:41,slotId:null,characteristicId:51+i,sequenceNo:i,unit:'g/L',usl:0.4,lsl:0.2,ucl:null,cl:null,lcl:null,targetValue:0.3,sampleSize:1,displayMode:'TREND_CHART',chartTypeId:null,formulaConfigJson:'{ "原文": "Xbar + A2 * Rbar" }',chemicalAnalysisConfigJson:JSON.stringify(config),isRequired:true,isEnabled:i!==11,machine:{machineCode:i<6?'LN-A':'LN-B'},tank:{tankCode:'T-RAW',tankName:raw},characteristic:{characteristicName:`${raw}-${i+1}`,characteristicCode:`C-${i+1}`}}));
const cells=[{cellAddress:'F!B3',standardSolution:'1N NaOH',numericValue:1.02},{cellAddress:'F!B4',standardSolution:raw,numericValue:0.983}];
const versions=Array.from({length:21},(_,i)=>({id:100+i,versionNo:30-i,versionCode:`F-RAW-${i}`,displayName:raw,changeType:'Updated',reason:raw,changedBy:raw,changedAt:'2026-10-24T04:05:00Z',configJson:JSON.stringify(config)}));
const errors=new WeakMap<Page,string[]>();
test.beforeEach(({page})=>{const list:string[]=[];errors.set(page,list);page.on('pageerror',e=>list.push(e.message))});
test.afterEach(({page})=>expect(errors.get(page)).toEqual([]));
async function prepare(page:Page,path='chemical-analysis-overview',role='Editor'){
 await page.addInitScript(role=>{localStorage.setItem('mes_spc_token','mock-ui-only-token');localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role,username:'chemical-fixture'}))},role);
 const writes:any[]=[];const reads:string[]=[];const state={empty:false,reject:false,rejectIds:[] as number[],noActive:false,historyEmpty:false,failHistory:false,failLoad:false,impactEnglish:null as string|null,records:structuredClone(items)};
 await page.route('**/*',async route=>{
  const u=new URL(route.request().url());if(u.origin!==new URL(test.info().project.use.baseURL as string).origin)return route.abort();
  if(!u.pathname.startsWith('/api/'))return route.continue();const method=route.request().method();
  if(method!=='GET'){
   expect(u.pathname).toMatch(/^\/api\/(part-process-characteristics\/\d+|v1\/chemical-f-table\/(apply|evaluate|versions\/\d+\/restore))$/);
   const body=route.request().postDataJSON();writes.push({method,path:u.pathname,body});const id=Number(u.pathname.split('/').at(-1));
   if(state.reject||state.rejectIds.includes(id))return route.fulfill({status:409,json:{message:'原 API 衝突訊息 <b>@:key {name}</b>'}});
   if(u.pathname.includes('part-process-characteristics')){const row=state.records.find(x=>x.id===id);if(row)Object.assign(row,body);return route.fulfill({json:{id,...body}})}
   if(u.pathname.endsWith('/evaluate'))return route.fulfill({json:{value:10.2,versionCode:'F-RAW-0'}});
   return route.fulfill({json:{versionCode:body.versionCode||'F-RAW-0',cells:[{action:'Updated'},{action:'Unchanged'}]}});
  }
  reads.push(u.pathname+u.search);let data:any=[];
  if(u.pathname.endsWith('/version'))data={version:'0.1.72',environment:'test'};
  else if(u.pathname==='/api/part-process-characteristics'){
   if(state.failLoad)return route.fulfill({status:500,json:{message:'原 API 載入錯誤'}});data=state.empty?[]:state.records;
  }else if(u.pathname.endsWith('/chemical-analysis-formula-versions')){
   if(state.failHistory)return route.fulfill({status:500,json:{message:'原 API 歷史錯誤'}});data={data:state.historyEmpty?[]:versions};
  }else if(u.pathname.endsWith('/chemical-f-table/active')){
   if(state.noActive)return route.fulfill({status:404,json:{message:'Not found'}});data={versionCode:'F-RAW-0',displayName:raw,effectiveAt:'2026-10-24T04:05:00Z',cells};
  }else if(u.pathname.endsWith('/chemical-f-table/versions')){
   if(state.failHistory)return route.fulfill({status:500,json:{message:'原 API 歷史錯誤'}});data=state.historyEmpty?[]:versions;
  }else if(u.pathname.endsWith('/chemical-f-table/impact'))data=state.empty?[]:[{partProcessCharacteristicId:1,referenceContext:'Concentration',machineName:raw,machineCode:'LN-A',tankName:raw,tankCode:'T-RAW',characteristicName:raw,...(state.impactEnglish?{characteristicNameEn:state.impactEnglish}:{}),characteristicCode:'C-1',sourceFormula:config.concentrationFormula}];
  return route.fulfill({json:data});
 });
 await page.goto('/'+path+'?keep=42');return {writes,reads,state};
}
const titles:any={'chemical-analysis-overview':{'zh-TW':'線別分析項目總覽','zh-CN':'线别分析项目总览','en-US':'Line analysis overview'},'chemical-f-table':{'zh-TW':'藥液 F 表維護','zh-CN':'药液 F 表维护','en-US':'Chemical F table'}};
for(const path of Object.keys(titles))for(const width of [1920,1366,1280,390])for(const locale of ['zh-TW','zh-CN','en-US'])test(`${path} ${width} ${locale}: fixed UI and usable inputs`,async({page})=>{
 await page.setViewportSize({width,height:900});const {writes}=await prepare(page,path);await page.getByTestId('locale-select').selectOption(locale);
 await expect(page.locator('main').getByRole('heading',{name:titles[path][locale],exact:true})).toBeVisible();
 const input=page.getByTestId(path==='chemical-analysis-overview'?'overview-formulaVersionReason':'f-code');await input.click();expect((await page.locator('main').boundingBox())!.x+(await page.locator('main').boundingBox())!.width).toBeLessThanOrEqual(width);expect((await input.boundingBox())!.width).toBeGreaterThan(width===390?120:150);
 if(path==='chemical-analysis-overview'){await page.getByTestId('overview-edit').first().click();await expect(page.getByTestId('overview-concentrationFormula')).toHaveValue(config.concentrationFormula)}
 else {await expect(page.getByTestId('f-solution').last()).toHaveValue(raw);expect((await page.getByTestId('f-solution').first().boundingBox())!.width).toBeGreaterThan(100);await page.getByTestId('f-preview').scrollIntoViewIfNeeded();await expect(page.getByTestId('f-preview')).toBeInViewport()}
 expect(await page.locator('main').evaluate(el=>el.scrollWidth)).toBeLessThanOrEqual(await page.locator('main').evaluate(el=>el.clientWidth));
 expect(writes).toEqual([]);
 if(width===390&&locale==='en-US')await page.screenshot({path:`../../.codex-tmp/i18n-task05c2/${path}-mobile-en.png`});
});
for(const locale of ['zh-TW','zh-CN','en-US']){
 test(`overview ${locale}: cross-page drafts, filters and exact batch DTO`,async({page})=>{
  const {writes,reads}=await prepare(page);await page.getByTestId('overview-pageSize').selectOption('5');await page.getByTestId('overview-line').selectOption('LN-A');await page.getByTestId('overview-status').selectOption('enabled');
  const row1=page.getByTestId('overview-row-1');await row1.getByTestId('overview-edit').click();await row1.getByTestId('overview-concentrationFormula').fill(' Primary * F!B4 + 0.2 ');await row1.getByTestId('overview-decimalPlaces').fill('0');
  await page.getByTestId('overview-formulaVersionReason').fill(' WI-RAW ｜ Rev.03 ｜ '+raw+' ');await page.getByTestId('overview-next').click();const row6=page.getByTestId('overview-row-6');await row6.getByTestId('overview-edit').click();await row6.getByTestId('overview-adjustmentAmountFormula').fill(' Primary * 3 ');await row6.getByTestId('overview-decimalPlaces').fill('8');
  const handle=await row6.getByTestId('overview-adjustmentAmountFormula').elementHandle();const before=reads.length;const url=page.url();await page.getByTestId('locale-select').selectOption(locale);expect(await handle!.evaluate(el=>el.isConnected)).toBe(true);expect(reads.length).toBe(before);expect(page.url()).toBe(url);await expect(page.getByTestId('overview-line')).toHaveValue('LN-A');await expect(page.getByTestId('overview-pageSize')).toHaveValue('5');await expect(row6).toBeVisible();
  let prompt='';page.once('dialog',async d=>{prompt=d.message();await d.accept()});await page.getByTestId('overview-save').click();await expect.poll(()=>writes.length).toBe(2);await expect(page.getByTestId('overview-save')).toBeDisabled();expect(prompt).toContain('LN-A');expect(prompt).toContain(raw);
  for(const [index,id] of [1,6].entries()){
   const {id:unused,machine,tank,characteristic,...body}=items[id-1];const nextConfig={...config,...(id===1?{concentrationFormula:'Primary * F!B4 + 0.2',decimalPlaces:0}:{adjustmentAmountFormula:'Primary * 3',decimalPlaces:8})};
   expect(writes[index]).toEqual({method:'PUT',path:`/api/part-process-characteristics/${id}`,body:{...body,chemicalAnalysisConfigJson:JSON.stringify(nextConfig),chemicalAnalysisFormulaVersionReason:(' WI-RAW ｜ Rev.03 ｜ '+raw+' ').trim()}});
  }
  await expect(page.locator('main b')).toHaveCount(0);
 });
 test(`overview ${locale}: partial failure preserves failed draft and original errors`,async({page})=>{
  const {writes,state}=await prepare(page);await page.getByTestId('overview-edit').first().click();await page.getByTestId('overview-concentrationFormula').fill('Primary * 4');await page.getByTestId('overview-row-2').getByTestId('overview-decimalPlaces').fill('8');await page.getByTestId('overview-formulaVersionReason').fill('原原因');state.rejectIds=[1];
  page.once('dialog',d=>d.accept());await page.getByTestId('overview-save').click();await expect(page.getByTestId('overview-row-1')).toContainText('原 API 衝突訊息');await expect(page.getByTestId('overview-concentrationFormula')).toHaveValue('Primary * 4');
  await page.getByTestId('locale-select').selectOption(locale);await expect(page.getByTestId('overview-formulaVersionReason')).toHaveValue('原原因');await page.getByTestId('locale-select').selectOption('en-US');await expect(page.locator('main')).toContainText('Save complete. Succeeded: 1; failed: 1.');await expect(page.locator('main')).toContainText('Some chemical formulas could not be saved.');
  state.rejectIds=[];page.once('dialog',d=>d.accept());await page.getByTestId('overview-save').click();await expect(page.getByTestId('overview-save')).toBeDisabled();expect(writes.map(x=>x.path)).toEqual(['/api/part-process-characteristics/1','/api/part-process-characteristics/2','/api/part-process-characteristics/1']);
 });
 test(`F table ${locale}: raw drafts, preview/apply/evaluate/impact/restore contracts`,async({page})=>{
  const {reads,writes}=await prepare(page,'chemical-f-table');await page.getByTestId('f-code').fill('F-NEW');await page.getByTestId('f-name').fill(raw);await page.getByTestId('f-value').first().fill('0.2');await page.getByTestId('f-formula').fill('Primary * F!B4 + 0.2');await page.getByTestId('f-primary').fill('0.2');await page.getByTestId('f-cell').selectOption('F!B4');
  const handle=await page.getByTestId('f-code').elementHandle();const before=reads.length;await page.getByTestId('locale-select').selectOption(locale);expect(await handle!.evaluate(el=>el.isConnected)).toBe(true);expect(reads.length).toBe(before);expect(writes).toEqual([]);await expect(page.getByTestId('f-cell')).toHaveValue('F!B4');
  await page.getByTestId('f-preview').click();await expect.poll(()=>writes.length).toBe(1);await page.getByTestId('locale-select').selectOption('en-US');await expect(page.locator('main')).toContainText('Dry-run complete. Cells to change: 1.');
  const body={apply:false,versionCode:'F-NEW',displayName:raw,cells:[{...cells[0],numericValue:0.2},cells[1]]};expect(writes[0]).toEqual({method:'POST',path:'/api/v1/chemical-f-table/apply',body});
  await page.getByTestId('f-evaluate').click();await expect(page.locator('main')).toContainText('10.2');expect(writes[1]).toEqual({method:'POST',path:'/api/v1/chemical-f-table/evaluate',body:{expression:'Primary * F!B4 + 0.2',variables:{Primary:0.2}}});
  await page.getByTestId('f-impact').click();await expect.poll(()=>reads.some(x=>x.includes('impact?cellAddress=F!B4')||x.includes('impact?cellAddress=F%21B4'))).toBe(true);
  await page.getByTestId('locale-select').selectOption(locale);await page.getByTestId('f-apply').click();await expect(page.getByTestId('f-name')).toHaveValue('F 表新版');expect(writes[2]).toEqual({method:'POST',path:'/api/v1/chemical-f-table/apply',body:{...body,apply:true}});
  let prompt='';page.once('dialog',async d=>{prompt=d.message();await d.dismiss()});await page.getByTestId('f-restore').first().click();expect(prompt).toContain('F-RAW-0');expect(writes).toHaveLength(3);
  page.once('dialog',d=>d.accept());await page.getByTestId('f-restore').first().click();await expect.poll(()=>writes.length).toBe(4);expect(writes[3]).toEqual({method:'POST',path:'/api/v1/chemical-f-table/versions/100/restore',body:{reason:'由畫面回復版本 30'}});
 });
 for(const path of Object.keys(titles))test(`history ${path} ${locale}: dates, paging, raw snapshots and no reload on switch`,async({page})=>{
  const {reads,writes,state}=await prepare(page,path);if(path==='chemical-analysis-overview')await page.getByTestId('overview-history').first().click();
  const from=page.getByTestId('history-from'),to=page.getByTestId('history-to');await from.fill('2026-10-24');await to.fill('2026-10-01');const before=reads.length;await page.getByTestId('history-search').click();await expect(page.getByRole('alert')).toBeVisible();expect(reads.length).toBe(before);
  await page.getByTestId('locale-select').selectOption('en-US');await expect(page.getByRole('alert')).toHaveText('Start date must not be later than end date.');await to.fill('');await page.getByTestId('history-search').click();await expect.poll(()=>reads.filter(x=>x.includes('fromDate=2026-10-24')).length).toBe(1);
  const count=reads.length;const handle=await from.elementHandle();await page.getByTestId('locale-select').selectOption(locale);expect(reads.length).toBe(count);expect(await handle!.evaluate(el=>el.isConnected)).toBe(true);await expect(from).toHaveValue('2026-10-24');
  await page.getByTestId('history-next').click();await expect.poll(()=>reads.some(x=>x.includes('skip=20')&&x.includes('take=21')&&x.includes('fromDate=2026-10-24'))).toBe(true);
  if(path==='chemical-analysis-overview'){await page.locator('details summary').first().click();await expect(page.locator('details pre').first()).toHaveText(JSON.stringify(config));await expect(page.locator('main')).toContainText('2026/10/24');}
  state.historyEmpty=true;await page.getByTestId('history-clear').click();await expect(from).toHaveValue('');await expect(to).toHaveValue('');await expect(page.getByTestId('history-next')).toBeDisabled();expect(writes).toEqual([]);
 });
}
test('overview reason length, confirm cancel/reset and unchanged XLSX protocol',async({page})=>{
 const {writes}=await prepare(page);await page.getByTestId('overview-edit').first().click();await page.getByTestId('overview-concentrationFormula').fill('Primary * 99');const reason=page.getByTestId('overview-formulaVersionReason');await reason.evaluate(el=>el.removeAttribute('maxlength'));await reason.fill('a'.repeat(501));await page.getByTestId('overview-save').click();await page.getByTestId('locale-select').selectOption('en-US');await expect(page.locator('main')).toContainText('must not exceed 500 characters');expect(writes).toEqual([]);
 await reason.fill('a'.repeat(500));page.once('dialog',d=>d.dismiss());await page.getByTestId('overview-save').click();await expect(page.getByTestId('overview-concentrationFormula')).toHaveValue('Primary * 99');await page.getByTestId('overview-reset').click();await expect(page.getByTestId('overview-concentrationFormula')).toHaveValue(config.concentrationFormula);await expect(page.getByTestId('overview-save')).toBeDisabled();
 await page.getByTestId('overview-line').selectOption('LN-B');const downloadPromise=page.waitForEvent('download');await page.getByTestId('overview-export').click();const download=await downloadPromise;expect(download.suggestedFilename()).toMatch(/^SPC_藥液分析項目總覽_\d{4}-\d{2}-\d{2}\.xlsx$/);const file=await download.path();const book=XLSX.read(await readFile(file!));expect(book.SheetNames).toEqual(['藥液分析項目']);const rows:any[]=XLSX.utils.sheet_to_json(book.Sheets[book.SheetNames[0]]);expect(rows).toHaveLength(6);expect(Object.keys(rows[0])).toEqual(['線別','槽體','槽體代碼','分析項目','狀態','滴定值2','濃度公式','調整公式','調整量公式','小數位','是否變更','設定警示']);expect(rows[0]['濃度公式']).toBe(config.concentrationFormula);expect(rows[0]['狀態']).toBe('啟用');expect(writes).toEqual([]);
});
test('overview bulk confirmation retains preview cap and more count',async({page})=>{
 const {writes}=await prepare(page);await page.getByTestId('overview-pageSize').selectOption('20');for(const input of await page.getByTestId('overview-decimalPlaces').all())await input.fill('8');await page.getByTestId('locale-select').selectOption('en-US');let prompt='';page.once('dialog',async d=>{prompt=d.message();await d.dismiss()});await page.getByTestId('overview-save').click();expect(prompt).toContain('12 records');expect(prompt).toContain('plus 4 more records');expect(prompt.split('\n').filter(x=>x.startsWith('- '))).toHaveLength(8);expect(writes).toEqual([]);
});
test('F table first-version defaults and API error text survive language switching',async({page})=>{
 const {state,writes}=await prepare(page,'chemical-f-table');state.noActive=true;state.empty=true;await page.reload();await page.getByTestId('locale-select').selectOption('en-US');await expect(page.locator('main')).toContainText('Default values for the first version have been loaded.');await expect(page.getByTestId('f-name')).toHaveValue('F 表新版');await expect(page.getByTestId('f-solution')).toHaveCount(8);await expect(page.getByTestId('f-solution').first()).toHaveValue('1N NaOH');await expect(page.getByTestId('f-value').first()).toHaveValue('1.02');
 state.reject=true;await page.getByTestId('f-preview').click();await expect(page.locator('main')).toContainText('原 API 衝突訊息 <b>@:key {name}</b>');await page.getByTestId('locale-select').selectOption('zh-CN');await expect(page.locator('main')).toContainText('原 API 衝突訊息');expect(writes[0].body.cells.map((x:any)=>x.cellAddress)).toEqual(['F!B3','F!B4','F!B5','F!B6','F!B7','F!B8','F!B9','F!B10']);
});
for(const path of Object.keys(titles))test(`${path}: empty/error responses`,async({page})=>{
 const {state,writes}=await prepare(page,path);state.empty=true;state.historyEmpty=true;await page.reload();await page.getByTestId('locale-select').selectOption('en-US');await expect(page.locator('main')).toContainText(path==='chemical-analysis-overview'?'No matching chemical analysis items.':'No F table versions');expect(writes).toEqual([]);
 if(path==='chemical-analysis-overview'){state.failLoad=true;await page.reload();await expect(page.locator('main')).toContainText('原 API 載入錯誤')}

});

for(const path of Object.keys(titles))test(`${path}: Viewer keeps existing route guard`,async({page})=>{
 const {writes}=await prepare(page,path,'Viewer');await expect(page).not.toHaveURL(new RegExp('/'+path+'\\?keep=42$'));expect(writes).toEqual([]);
});
test('formula history API failure keeps original text and retry context across languages',async({page})=>{
 const {state,writes}=await prepare(page);state.failHistory=true;await page.getByTestId('overview-history').first().click();await expect(page.getByRole('alert')).toContainText('原 API 歷史錯誤');await page.getByTestId('locale-select').selectOption('en-US');await expect(page.getByRole('alert')).toContainText('原 API 歷史錯誤');state.failHistory=false;await page.getByRole('button',{name:'Retry',exact:true}).click();await expect(page.locator('article')).toHaveCount(20);expect(writes).toEqual([]);
});

for(const width of [390,1280])test(`chemical names ${width}: overview history draft and Excel change only name`,async({page})=>{
 await page.setViewportSize({width,height:900});const {reads,writes,state}=await prepare(page);const english='=1+1 <b>@:key {name}</b>';state.records[0].characteristic={...state.records[0].characteristic,characteristicNameEn:english};await page.reload();const row=page.getByTestId('overview-row-1');await row.getByTestId('overview-edit').click();await row.getByTestId('overview-concentrationFormula').fill('Primary * 3');await row.getByTestId('overview-history').click();await expect(page.locator('article')).toHaveCount(20);const count=reads.length,handle=await row.getByTestId('overview-concentrationFormula').elementHandle();
 for(const [locale,name] of [['en-US',english],['zh-CN',raw+'-1'],['zh-TW',raw+'-1']]){await page.getByTestId('locale-select').selectOption(locale);await expect(row.getByTestId('overview-characteristic-name')).toHaveText(name);await expect(page.getByRole('heading',{level:2}).filter({hasText:name})).toHaveCount(1);await expect(row.getByTestId('overview-concentrationFormula')).toHaveValue('Primary * 3');expect(await handle!.evaluate(n=>n.isConnected)).toBe(true);expect(reads.length).toBe(count)}
 await page.getByTestId('locale-select').selectOption('en-US');const downloadPromise=page.waitForEvent('download');await page.getByTestId('overview-export').click();const download=await downloadPromise;expect(download.suggestedFilename()).toMatch(/^SPC_藥液分析項目總覽_/);const book=XLSX.read(await readFile((await download.path())!));expect(book.SheetNames).toEqual(['藥液分析項目']);const sheet=book.Sheets[book.SheetNames[0]],data:any[]=XLSX.utils.sheet_to_json(sheet);expect(data[0]['分析項目']).toBe(english);expect(data[0]['濃度公式']).toBe('Primary * 3');expect(data[0]['小數位']).toBe(2);expect(sheet.D2.t).toBe('s');expect(sheet.D2.f).toBeUndefined();await expect(row.locator('b')).toHaveCount(0);expect(writes).toEqual([]);
});
for(const width of [390,1280])test(`chemical names ${width}: F impact keeps reference and cell selection`,async({page})=>{
 await page.setViewportSize({width,height:900});const {reads,writes,state}=await prepare(page,'chemical-f-table');state.impactEnglish='Proportion <b>@:key {name}</b>';await page.reload();await expect(page.getByTestId('f-impact-characteristic-name')).toHaveCount(1);const count=reads.length;
 for(const [locale,name] of [['en-US',state.impactEnglish],['zh-CN',raw],['zh-TW',raw]]){await page.getByTestId('locale-select').selectOption(locale);await expect(page.getByTestId('f-impact-characteristic-name')).toHaveText(name);await expect(page.getByTestId('f-cell')).toHaveValue('F!B3');expect(reads.length).toBe(count)}
 await expect(page.getByTestId('f-impact-characteristic-name').locator('b')).toHaveCount(0);expect(writes).toEqual([]);
});
test('chemical names missing English: both remaining pages fall back to original name',async({page})=>{
 await prepare(page);await page.getByTestId('locale-select').selectOption('en-US');await expect(page.getByTestId('overview-characteristic-name').first()).toHaveText(raw+'-1');await page.goto('/chemical-f-table?keep=42');await expect(page.getByTestId('f-impact-characteristic-name')).toHaveText(raw);
});
